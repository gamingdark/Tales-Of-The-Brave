using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TalesOfVoyages.Graphics;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.Entities;
using TalesOfVoyages.Simulation.Rulesets;
using TalesOfVoyages.Simulation.World;
using TalesOfVoyages.Simulation.Time;

public sealed class MvpWorldDefinitionTests
{
    [Test]
    public void DefaultDefinitionContainsRequiredMvpEntitiesWithoutRequiringExactCounts()
    {
        var definition = MvpWorldDefinition.CreateDefault();
        var game = MvpGameFactory.Create(definition);

        AssertNode(game.World.GetNode("node_klaipeda"), "Klaipėda", 0.62f, 0.2f);
        AssertNode(game.World.GetNode("node_riga"), "Riga", 0.73f, 0.33f);
        AssertNode(game.World.GetNode("node_helsinki"), "Helsinki", 0.70f, 0.63f);
        Assert.That(game.World.GetEdge("route_klaipeda_courland").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_courland_irbe").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_irbe_riga").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_courland_saaremaa").Distance, Is.EqualTo(55f));
        Assert.That(game.World.GetEdge("route_irbe_saaremaa").Distance, Is.EqualTo(25f));
        Assert.That(game.World.GetEdge("route_saaremaa_helsinki").Distance, Is.EqualTo(40f));

        AssertPort(game, "port_klaipeda", "node_klaipeda", "icons.4", "img-klaipeda");
        AssertPort(game, "port_riga", "node_riga", "icons.3", "img-riga");
        AssertPort(game, "port_helsinki", "node_helsinki", "icons.5", "img-helsinki");
        Assert.That(game.PlayerShip.Id, Is.EqualTo("player_ship"));
        Assert.That(game.PlayerShip.DisplayName, Is.EqualTo("The Unsinkable MVP"));
        Assert.That(game.PlayerShip.SpeedPerDay, Is.EqualTo(25f));
        Assert.That(game.PlayerShip.Entity.HasBehavior<PlayerControlledBehavior>(), Is.True);
        Assert.That(game.PlayerShip.Entity.GetBehavior<TransportBehavior>().SpeedPerDay, Is.EqualTo(25f));
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("node_klaipeda"));
        Assert.That(game.PlayerShip.Entity.GetBehavior<DrawableBehavior>().MapIconSprite, Is.EqualTo("icons.8"));
        Assert.That(game.Time.SecondsPerDay, Is.EqualTo(7.5f));
        Assert.That(game.Time.CurrentDate.DaysPerMonth, Is.EqualTo(30));
        Assert.That(game.Time.HoursPerDay, Is.EqualTo(24));
        Assert.That(game.Time.DayStartHourOffset, Is.EqualTo(7));
        Assert.That(game.Time.AllowedSpeeds,
            Is.EqualTo(new[] { TimeSpeed.Normal, TimeSpeed.Fast, TimeSpeed.VeryFast }));
        Assert.That(definition.MapBackgroundSprite, Is.EqualTo("map"));
        Assert.That(definition.SceneBackgroundSprite, Is.EqualTo("wooden-background"));
    }

    [Test]
    public void CreationIsUnaffectedByAdditionalValidNodeAndEntity()
    {
        var definition = MvpWorldDefinition.CreateDefault();
        definition.Nodes.Add(new WorldNodeDefinition("node_extra", "Extra", WorldNodeType.Port, 0.1f, 0.1f));
        definition.Entities.Add(CreatePort("port_extra", "Extra", "node_extra", "icons.extra"));

        Assert.DoesNotThrow(() => MvpGameFactory.Create(definition));
    }

    [Test]
    public void CreationRejectsDuplicateIds()
    {
        var definition = CreateMinimalDefinition();
        definition.Nodes.Add(new WorldNodeDefinition("node_a", "Duplicate", WorldNodeType.Port, 0f, 0f));

        AssertValidationFailure(definition, "Duplicate definition ID");
    }

    [Test]
    public void CreationRejectsMissingEdgeEndpointNode()
    {
        var definition = CreateMinimalDefinition();
        definition.Edges[0].NodeBId = "node_missing";

        AssertValidationFailure(definition, "missing endpoint node");
    }

    [Test]
    public void CreationRejectsInvalidEdgeDistance()
    {
        var definition = CreateMinimalDefinition();
        definition.Edges[0].Distance = 0f;

        AssertValidationFailure(definition, "positive distance");
    }

    [Test]
    public void CreationRejectsInvalidEntityStartingNode()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.WorldEntityBehavior.StartingNodeId = "node_missing";

        AssertValidationFailure(definition, "does not exist");
    }

    [Test]
    public void CreationRejectsMultiplePlayerControlledEntities()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities.Add(CreatePlayer("other_player", "node_b", "icons.other"));

        AssertValidationFailure(definition, "Exactly one player-controlled entity");
    }

    [Test]
    public void ValidationRejectsPlayerControlledEntityWithoutTransportBehavior()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.TransportBehavior = null;

        AssertValidationFailure(definition, "requires a transport behavior");
    }

    [Test]
    public void ValidationRejectsInvalidTransportSpeed()
    {
        var definition = CreateMinimalDefinition();
        definition.Entities[0].Behaviors.TransportBehavior.SpeedPerDay = 0f;

        AssertValidationFailure(definition, "positive speed");
    }

    [Test]
    public void GraphicsValidationAcceptsAllDefaultSpriteNames()
    {
        var sprites = new SpriteNames(
            "icons.3", "icons.4", "icons.5", "icons.8",
            "img-klaipeda", "img-riga", "img-helsinki", "map", "wooden-background");
        Assert.DoesNotThrow(() => MvpWorldDefinitionValidator.Validate(MvpWorldDefinition.CreateDefault(), sprites));
    }

    [Test]
    public void GraphicsValidationRejectsMissingMapBackgroundSprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MvpWorldDefinitionValidator.Validate(
                definition, new SpriteNames("icons.ship", "icons.port", "img-port", "wooden-background")));

        StringAssert.Contains("map", exception.Message);
    }

    [Test]
    public void GraphicsValidationRejectsMissingSceneBackgroundSprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MvpWorldDefinitionValidator.Validate(
                definition, new SpriteNames("map", "icons.ship", "icons.port", "img-port")));

        StringAssert.Contains("wooden-background", exception.Message);
    }

    [Test]
    public void GraphicsValidationRejectsMissingEntitySprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MvpWorldDefinitionValidator.Validate(
                definition, new SpriteNames("map", "wooden-background", "icons.port")));

        StringAssert.Contains("player_ship", exception.Message);
        StringAssert.Contains("icons.ship", exception.Message);
    }

    [Test]
    public void GraphicsValidationRejectsMissingPortViewSprite()
    {
        var definition = CreateMinimalDefinition();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MvpWorldDefinitionValidator.Validate(
                definition, new SpriteNames("map", "wooden-background", "icons.ship", "icons.port")));

        StringAssert.Contains("port_b", exception.Message);
        StringAssert.Contains("img-port", exception.Message);
    }

    [Test]
    public void EnteringPortQueryReturnsDestinationPortEntity()
    {
        var game = MvpGameFactory.Create();
        game.Movement.PlanDestination(game.PlayerShip.Id, "node_helsinki");
        game.Time.AdvanceDays(5);
        game.Time.Tick(game.Time.SecondsPerDay * 0.9f);

        var port = game.GetPendingInteractionEntity();

        Assert.That(port.Id, Is.EqualTo("port_helsinki"));
        Assert.That(port.GetBehavior<PortBehavior>().PortViewSprite, Is.EqualTo("img-helsinki"));
        Assert.That(port.Actions.Select(action => action.Label), Is.EqualTo(new[] { "Enter port" }));
    }

    [Test]
    public void FactoryUsesCustomTimeSystemDefinition()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem = new TimeSystemDefinition
        {
            SecondsPerDay = 42f,
            DaysPerMonth = 18,
            HoursPerDay = 12,
            DayStartHourOffset = 4,
            AllowedSpeeds = new List<TimeSpeed> { TimeSpeed.Fast }
        };

        var game = MvpGameFactory.Create(definition);

        Assert.That(game.Time.SecondsPerDay, Is.EqualTo(42f));
        Assert.That(game.Time.CurrentDate.DaysPerMonth, Is.EqualTo(18));
        Assert.That(game.Time.HoursPerDay, Is.EqualTo(12));
        Assert.That(game.Time.GetFormattedTime(), Is.EqualTo("04:00"));
        Assert.That(game.Time.Speed, Is.EqualTo(TimeSpeed.Fast));
    }

    [TestCase(0, 24, 7, "day length")]
    [TestCase(7.5f, 0, 7, "days per month")]
    [TestCase(7.5f, 30, -1, "day-start offset")]
    public void ValidationRejectsInvalidTimeSettings(
        float secondsPerDay, int daysPerMonth, int dayStartOffset, string expectedMessage)
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.SecondsPerDay = secondsPerDay;
        definition.TimeSystem.DaysPerMonth = daysPerMonth;
        definition.TimeSystem.DayStartHourOffset = dayStartOffset;

        AssertValidationFailure(definition, expectedMessage);
    }

    [Test]
    public void ValidationRejectsPauseAsConfiguredRunningSpeed()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.AllowedSpeeds = new List<TimeSpeed> { TimeSpeed.Paused };

        AssertValidationFailure(definition, "cannot be configured");
    }

    [Test]
    public void ValidationRejectsInvalidHoursPerDay()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.HoursPerDay = 0;

        AssertValidationFailure(definition, "hours per day");
    }

    [Test]
    public void ValidationRejectsDuplicateRunningSpeeds()
    {
        var definition = CreateMinimalDefinition();
        definition.TimeSystem.AllowedSpeeds = new List<TimeSpeed> { TimeSpeed.Normal, TimeSpeed.Normal };

        AssertValidationFailure(definition, "more than once");
    }

    private static MvpWorldDefinition CreateMinimalDefinition()
    {
        return new MvpWorldDefinition
        {
            Nodes = new List<WorldNodeDefinition>
            {
                new WorldNodeDefinition("node_a", "A", WorldNodeType.Port, 0f, 0f),
                new WorldNodeDefinition("node_b", "B", WorldNodeType.Port, 1f, 1f)
            },
            Edges = new List<WorldEdgeDefinition>
            {
                new WorldEdgeDefinition("edge_ab", "node_a", "node_b", 10f)
            },
            Entities = new List<EntityDefinition>
            {
                CreatePlayer("player_ship", "node_a", "icons.ship"),
                CreatePort("port_b", "B", "node_b", "icons.port")
            }
        };
    }

    private static EntityDefinition CreatePlayer(string id, string nodeId, string sprite) =>
        new PlayerShipDefinition(id, "Ship", 25f, nodeId, sprite);

    private static EntityDefinition CreatePort(string id, string name, string nodeId, string sprite) =>
        new EntityDefinition(id, name, new EntityBehaviorsDefinition
        {
            PortBehavior = new PortBehaviorDefinition("img-port"),
            DrawableBehavior = new DrawableBehaviorDefinition(sprite),
            WorldEntityBehavior = new WorldEntityBehaviorDefinition(nodeId)
        });

    private static void AssertValidationFailure(MvpWorldDefinition definition, string message)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => MvpGameFactory.Create(definition));
        StringAssert.Contains(message, exception.Message);
    }

    private static void AssertPort(
        GameContext game, string id, string nodeId, string sprite, string portViewSprite)
    {
        var port = game.Entities.Single(entity => entity.Id == id);
        Assert.That(port.HasBehavior<PortBehavior>(), Is.True);
        Assert.That(port.GetBehavior<WorldEntityBehavior>().StartingNodeId, Is.EqualTo(nodeId));
        Assert.That(port.GetBehavior<DrawableBehavior>().MapIconSprite, Is.EqualTo(sprite));
        Assert.That(port.GetBehavior<PortBehavior>().PortViewSprite, Is.EqualTo(portViewSprite));
    }

    private static void AssertNode(WorldNode node, string displayName, float mapX, float mapY)
    {
        Assert.That(node.DisplayName, Is.EqualTo(displayName));
        Assert.That(node.Type, Is.EqualTo(WorldNodeType.Port));
        Assert.That(node.MapX, Is.EqualTo(mapX));
        Assert.That(node.MapY, Is.EqualTo(mapY));
        Assert.That(node.IsDiscovered, Is.True);
    }

    private sealed class SpriteNames : ISpriteNameLookup
    {
        private readonly HashSet<string> names;
        public SpriteNames(params string[] names) => this.names = new HashSet<string>(names, StringComparer.Ordinal);
        public bool ContainsSprite(string name) => names.Contains(name);
    }
}
