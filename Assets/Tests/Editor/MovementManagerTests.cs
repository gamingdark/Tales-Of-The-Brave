using NUnit.Framework;
using System.Linq;
using TalesOfTheBrave.Simulation.Core;
using TalesOfTheBrave.Simulation.Movement;
using TalesOfTheBrave.Simulation.Time;
using TalesOfTheBrave.Simulation.World;

public sealed class MovementManagerTests
{
    [Test]
    public void ShipTravelsFromKlaipedaToRigaAndBack()
    {
        var game = GameFactory.Create();
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
        var game = GameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "node_riga");
        game.Time.AdvanceDay();
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.Zero);
        game.Time.SetSpeed(TalesOfTheBrave.Simulation.Time.TimeSpeed.Paused);
        game.Time.Tick(300f);
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.Zero);
        game.Time.SetSpeed(TalesOfTheBrave.Simulation.Time.TimeSpeed.VeryFast);
        game.Time.Tick(game.Time.SecondsPerDay / 4f);
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void PlannedVoyageCanBeCancelledBeforeTheNextDay()
    {
        var game = GameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "node_riga");
        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.True);

        game.Movement.CancelPlannedDestination(GameContext.PlayerShipId);
        game.Time.AdvanceDay();

        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.False);
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
    }

    [Test]
    public void ActiveRouteCanBeAbortedAtItsNextImmediateNode()
    {
        var game = GameFactory.Create();
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_riga");
        game.Time.AdvanceDay();
        game.Tick(game.Time.SecondsPerDay * 0.25f);

        game.Movement.AbortRoute(game.PlayerShip.Id, game.Time.DayProgress);

        Assert.That(game.PlayerShip.Travel.DestinationNodeId, Is.EqualTo("node_west_courland"));
        Assert.That(game.PlayerShip.Travel.RemainingRoute,
            Is.EqualTo(new[] { "node_west_courland" }));

        game.Tick(game.Time.SecondsPerDay * 0.75f);

        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_west_courland"));
        Assert.That(game.PlayerShip.Travel.Status, Is.EqualTo(TravelStatus.AtNode));
    }

    [Test]
    public void StationaryShipCanEnterAndExitCurrentLocation()
    {
        var game = GameFactory.Create();
        var location = game.GetLocationAtNode(game.PlayerShip.Travel.CurrentNodeId);
        var action = location.Actions.Single(candidate =>
            candidate.IsAvailable(game) && candidate.Label == "Go into Klaipėda");
        var initialDate = game.Time.CurrentDate;
        var initialProgress = game.Time.DayProgress;

        action.Execute(game);

        Assert.That(game.PlayerShip.Travel.Status, Is.EqualTo(TravelStatus.InsideLocation));
        Assert.That(game.GetInsideLocationEntity(), Is.SameAs(location));
        Assert.That(game.Time.Speed, Is.EqualTo(TimeSpeed.Paused));
        Assert.Throws<System.InvalidOperationException>(() =>
            game.Movement.PlanDestination(game.PlayerShip.Id, "node_riga"));

        game.Tick(game.Time.SecondsPerDay * 10f);
        Assert.That(game.Time.CurrentDate, Is.EqualTo(initialDate));
        Assert.That(game.Time.DayProgress, Is.EqualTo(initialProgress));

        game.ExitLocation();

        Assert.That(game.PlayerShip.Travel.Status, Is.EqualTo(TravelStatus.AtNode));
        Assert.That(game.GetInsideLocationEntity(), Is.Null);
        Assert.That(game.Time.Speed, Is.EqualTo(TimeSpeed.Normal));
    }

    [Test]
    public void IntermediateSeaNodeIsPassedWithoutStoppingOrInteraction()
    {
        var definition = TalesOfTheBrave.Simulation.Rulesets.WorldDefinition.CreateDefault();
        definition.Entities
            .Single(entity => entity.Behaviors.PlayerControlledBehavior != null)
            .Behaviors.TransportBehavior.SpeedPerDay = 50f;
        var game = GameFactory.Create(definition);
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
        var game = GameFactory.Create();
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
        var game = GameFactory.Create();
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
    public void MiddayArrivalRemainsEnteringLocationUntilNextDay()
    {
        var world = new WorldGraph();
        world.AddNode(new WorldNode("a", "A", WorldNodeType.Location, 0f, 0f));
        world.AddNode(new WorldNode("b", "B", WorldNodeType.Location, 1f, 0f));
        world.AddEdge(new WorldEdge("a_b", "a", "b", 10f));
        var movement = new MovementManager(world);
        var ship = new Transport("ship", "Ship", 20f, "a");
        movement.Register(ship);
        var time = new TimeManager(30f);
        time.DayAdvanced += _ => movement.ProcessDay();
        movement.PlanDestination("ship", "b");
        time.AdvanceDay();
        time.Tick(15f);

        Assert.That(ship.Travel.IsEnteringLocation(time.DayProgress), Is.True);
        Assert.That(ship.Travel.IsTravelling, Is.True);

        time.Tick(15f);
        Assert.That(ship.Travel.CurrentNodeId, Is.EqualTo("b"));
        Assert.That(ship.Travel.IsTravelling, Is.False);
    }

    [Test]
    public void ActionableArrivalStopsAtSimulationDayEndUntilEnterLocationActionRuns()
    {
        var game = GameFactory.Create();
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_helsinki");
        game.Time.AdvanceDays(5);

        game.Tick(game.Time.SecondsPerDay);

        var interaction = game.GetPendingInteractionEntity();
        Assert.That(interaction.Id, Is.EqualTo("location_helsinki"));
        var enterAction = interaction.Actions.Single(action => action.IsAvailable(game));
        Assert.That(enterAction.Label, Is.EqualTo("Enter location"));
        Assert.That(game.Time.DayProgress, Is.EqualTo(1f));
        Assert.That(game.Time.GetFormattedTime(), Is.EqualTo("06:00"));
        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(5));
        var stoppedProgress = game.Time.DayProgress;

        game.Tick(game.Time.SecondsPerDay * 10f);
        Assert.That(game.Time.DayProgress, Is.EqualTo(stoppedProgress));
        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(5));

        enterAction.Execute(game);

        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_helsinki"));
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        Assert.That(game.PlayerShip.Travel.Status, Is.EqualTo(TravelStatus.InsideLocation));
        Assert.That(game.GetInsideLocationEntity().Id, Is.EqualTo("location_helsinki"));
        Assert.That(game.Time.Speed, Is.EqualTo(TimeSpeed.Paused));
        Assert.That(game.Time.DayProgress, Is.Zero);
        Assert.That(game.Time.GetFormattedTime(), Is.EqualTo("07:00"));
        Assert.That(game.Time.CurrentDate.TotalDays, Is.EqualTo(6));
    }
}
