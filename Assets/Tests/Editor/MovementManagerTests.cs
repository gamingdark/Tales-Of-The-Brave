using NUnit.Framework;
using System.Linq;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;

public sealed class MovementManagerTests
{
    [Test]
    public void ShipTravelsFromKlaipedaToRigaAndBack()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "node_riga");
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        game.Time.AdvanceDays(5);
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_riga"));
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        game.Movement.PlanDestination(GameContext.PlayerShipId, "node_klaipeda");
        game.Time.AdvanceDays(5);
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_klaipeda"));
    }

    [Test]
    public void PartialProgressSurvivesPauseAndSpeedChanges()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "node_riga");
        game.Time.AdvanceDay();
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.Zero);
        game.Time.SetSpeed(TalesOfVoyages.Simulation.Time.TimeSpeed.Paused);
        game.Time.Tick(300f);
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.Zero);
        game.Time.SetSpeed(TalesOfVoyages.Simulation.Time.TimeSpeed.VeryFast);
        game.Time.Tick(game.Time.SecondsPerDay / 4f);
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void PlannedVoyageCanBeCancelledBeforeTheNextDay()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "node_riga");
        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.True);

        game.Movement.CancelPlannedDestination(GameContext.PlayerShipId);
        game.Time.AdvanceDay();

        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.False);
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
    }

    [Test]
    public void IntermediateSeaNodeIsPassedWithoutStoppingOrInteraction()
    {
        var definition = TalesOfVoyages.Simulation.Rulesets.MvpWorldDefinition.CreateDefault();
        definition.Entities
            .Single(entity => entity.Behaviors.PlayerControlledBehavior != null)
            .Behaviors.TransportBehavior.SpeedPerDay = 50f;
        var game = MvpGameFactory.Create(definition);
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_riga");
        game.Time.AdvanceDay();

        Assert.That(game.PlayerShip.Travel.NextNodeId, Is.EqualTo("node_west_courland"));

        game.Tick(game.Time.SecondsPerDay * 0.75f);

        Assert.That(
            game.PlayerShip.Travel.GetNextNodeId(game.Time.DayProgress),
            Is.EqualTo("node_irbe_strait"));
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.True);
        Assert.That(game.GetPendingInteractionEntity(), Is.Null);
        Assert.That(game.Time.DayProgress, Is.EqualTo(0.75f));

        game.Tick(game.Time.SecondsPerDay * 0.25f);

        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_irbe_strait"));
        Assert.That(game.PlayerShip.Travel.NextNodeId, Is.EqualTo("node_riga"));
        Assert.That(game.Time.DayProgress, Is.Zero);
    }

    [Test]
    public void SeaNodeCanBeFinalDestinationAndNewVoyageWaitsForFollowingDay()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_west_courland");
        game.Time.AdvanceDay();

        game.Tick(game.Time.SecondsPerDay);

        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_west_courland"));
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        Assert.That(game.GetPendingInteractionEntity(), Is.Null);

        game.Movement.PlanDestination(game.PlayerShip.Id, "node_irbe_strait");
        game.Tick(game.Time.SecondsPerDay * 0.5f);

        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.True);
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);

        game.Time.AdvanceDay();

        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.False);
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.True);
        Assert.That(game.PlayerShip.Travel.NextNodeId, Is.EqualTo("node_irbe_strait"));
    }

    [Test]
    public void SkipToNextDayStartsPlannedVoyageImmediately()
    {
        var game = MvpGameFactory.Create();
        game.Time.Tick(game.Time.SecondsPerDay * 0.5f);
        var initialDay = game.Time.CurrentDate.TotalDays;
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_riga");

        game.Time.SkipToNextDayStart();

        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(initialDay + 1));
        Assert.That(game.Time.DayProgress, Is.Zero);
        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.False);
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.True);
    }

    [Test]
    public void MiddayArrivalRemainsEnteringPortUntilNextDay()
    {
        var world = new WorldGraph();
        world.AddNode(new WorldNode("a", "A", WorldNodeType.Port, 0f, 0f));
        world.AddNode(new WorldNode("b", "B", WorldNodeType.Port, 1f, 0f));
        world.AddEdge(new WorldEdge("a_b", "a", "b", 10f));
        var movement = new MovementManager(world);
        var ship = new Transport("ship", "Ship", 20f, "a");
        movement.Register(ship);
        var time = new TimeManager(30f);
        time.DayAdvanced += _ => movement.ProcessDay();
        movement.PlanDestination("ship", "b");
        time.AdvanceDay();
        time.Tick(15f);

        Assert.That(ship.Travel.IsEnteringPort(time.DayProgress), Is.True);
        Assert.That(ship.Travel.IsTravelling, Is.True);

        time.Tick(15f);
        Assert.That(ship.Travel.CurrentNodeId, Is.EqualTo("b"));
        Assert.That(ship.Travel.IsTravelling, Is.False);
    }

    [Test]
    public void ActionableArrivalStopsAtSimulationDayEndUntilEnterPortActionRuns()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_helsinki");
        game.Time.AdvanceDays(5);

        game.Tick(game.Time.SecondsPerDay);

        var interaction = game.GetPendingInteractionEntity();
        Assert.That(interaction.Id, Is.EqualTo("port_helsinki"));
        Assert.That(interaction.Actions.Count, Is.EqualTo(1));
        Assert.That(interaction.Actions[0].Label, Is.EqualTo("Enter port"));
        Assert.That(game.Time.DayProgress, Is.EqualTo(1f));
        Assert.That(game.Time.GetFormattedTime(), Is.EqualTo("06:00"));
        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(5));
        var stoppedProgress = game.Time.DayProgress;

        game.Tick(game.Time.SecondsPerDay * 10f);
        Assert.That(game.Time.DayProgress, Is.EqualTo(stoppedProgress));
        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(5));

        interaction.Actions[0].Execute(game);

        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_helsinki"));
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        Assert.That(game.Time.DayProgress, Is.Zero);
        Assert.That(game.Time.GetFormattedTime(), Is.EqualTo("07:00"));
        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(6));
    }
}
