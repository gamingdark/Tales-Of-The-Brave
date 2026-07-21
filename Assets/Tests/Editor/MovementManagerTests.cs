using NUnit.Framework;
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
        game.Movement.PlanDestination(GameContext.PlayerShipId, "port_riga");
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        game.Time.AdvanceDays(5);
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("port_riga"));
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
        game.Movement.PlanDestination(GameContext.PlayerShipId, "port_klaipeda");
        game.Time.AdvanceDays(5);
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("port_klaipeda"));
    }

    [Test]
    public void PartialProgressSurvivesPauseAndSpeedChanges()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "port_riga");
        game.Time.AdvanceDay();
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.Zero);
        game.Time.SetSpeed(TalesOfVoyages.Simulation.Time.TimeSpeed.Paused);
        game.Time.Tick(300f);
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.Zero);
        game.Time.SetSpeed(TalesOfVoyages.Simulation.Time.TimeSpeed.VeryFast);
        game.Time.Tick(game.Time.SecondsPerDay / 4f);
        Assert.That(game.PlayerShip.Travel.GetVisualEdgeProgress(game.Time.DayProgress), Is.EqualTo(1f / 3f).Within(0.001f));
    }

    [Test]
    public void PlannedVoyageCanBeCancelledBeforeTheNextDay()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(GameContext.PlayerShipId, "port_riga");
        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.True);

        game.Movement.CancelPlannedDestination(GameContext.PlayerShipId);
        game.Time.AdvanceDay();

        Assert.That(game.PlayerShip.Travel.HasPlannedAction, Is.False);
        Assert.That(game.PlayerShip.Travel.IsTravelling, Is.False);
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
}
