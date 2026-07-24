using System.Linq;
using TalesOfTheBrave.Simulation.Chronicle;
using TalesOfTheBrave.Simulation.Movement;
using TalesOfTheBrave.Simulation.Rulesets;
using TalesOfTheBrave.Simulation.Time;
using TalesOfTheBrave.Simulation.World;
using TalesOfTheBrave.Simulation.Entities;
using System.Collections.Generic;

namespace TalesOfTheBrave.Simulation.Core
{
    public static class GameFactory
    {
        public static GameContext Create()
        {
            return Create(WorldDefinition.CreateDefault());
        }

        public static GameContext Create(WorldDefinition definition)
        {
            WorldDefinitionValidator.Validate(definition);

            var timeDefinition = definition.TimeSystem;
            var time = new TimeManager(
                timeDefinition.SecondsPerDay,
                daysPerMonth: timeDefinition.DaysPerMonth,
                hoursPerDay: timeDefinition.HoursPerDay,
                dayStartHourOffset: timeDefinition.DayStartHourOffset,
                allowedSpeeds: timeDefinition.AllowedSpeeds);
            var world = new WorldGraph();
            var commodities = definition.Commodities.ToDictionary(
                commodity => commodity.Name,
                commodity => new Commodity(
                    commodity.Name,
                    commodity.DefaultPrice,
                    commodity.Unit.FullName,
                    commodity.Unit.Abbreviation),
                System.StringComparer.Ordinal);

            foreach (var node in definition.Nodes)
                world.AddNode(new WorldNode(
                    node.Id,
                    node.DisplayName,
                    node.Type,
                    node.MapX,
                    node.MapY,
                    node.IsDiscovered));

            foreach (var edge in definition.Edges)
                world.AddEdge(new WorldEdge(
                    edge.Id,
                    edge.NodeAId,
                    edge.NodeBId,
                    edge.Distance,
                    edge.MapWaypoints?.Select(point => new WorldMapPoint(point.X, point.Y))));

            var entities = new List<Entity>();
            foreach (var entityDefinition in definition.Entities)
                entities.Add(CreateEntity(entityDefinition, commodities));

            var playerEntity = entities.Single(entity => entity.HasBehavior<PlayerControlledBehavior>());
            var movement = new MovementManager(world, nodeId => entities.Any(entity =>
                entity.Actions.Count > 0 &&
                entity.HasBehavior<WorldEntityBehavior>() &&
                entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == nodeId));
            movement.Register(new Transport(playerEntity));

            var chronicler = new Chronicler();
            var context = new GameContext(
                time, world, movement, chronicler, playerEntity.Id, entities, commodities);
            time.DayAdvanced += context.ProcessDay;
            movement.VoyageStarted += (ship, from, to) => chronicler.Record(
                time.CurrentDate,
                "VoyageStarted",
                $"We left {world.GetNode(from).DisplayName} for {world.GetNode(to).DisplayName} aboard {ship.DisplayName}.");
            movement.Arrived += (ship, at) => chronicler.Record(
                time.CurrentDate,
                "VoyageCompleted",
                $"We arrived safely at {world.GetNode(at).DisplayName}.");
            chronicler.Record(
                time.CurrentDate,
                "GameStarted",
                $"Our voyage begins in {world.GetNode(playerEntity.GetBehavior<WorldEntityBehavior>().StartingNodeId).DisplayName}.");
            return context;
        }

        private static Entity CreateEntity(
            EntityDefinition definition,
            IReadOnlyDictionary<string, Commodity> commodities)
        {
            var entity = new Entity(definition.Id, definition.DisplayName);
            var behaviors = definition.Behaviors;
            if (behaviors.PlayerControlledBehavior != null)
                entity.AddBehavior(new PlayerControlledBehavior());
            if (behaviors.TransportBehavior != null)
                entity.AddBehavior(new TransportBehavior(
                    behaviors.TransportBehavior.SpeedPerDay,
                    behaviors.TransportBehavior.MaxCargoAmount,
                    behaviors.TransportBehavior.CurrentGold));
            if (behaviors.DrawableBehavior != null)
                entity.AddBehavior(new DrawableBehavior(behaviors.DrawableBehavior.MapIconSprite));
            if (behaviors.WorldEntityBehavior != null)
                entity.AddBehavior(new WorldEntityBehavior(behaviors.WorldEntityBehavior.StartingNodeId));
            if (behaviors.LocationBehavior != null)
                entity.AddBehavior(new LocationBehavior(
                    behaviors.LocationBehavior.LocationViewSprite,
                    definition.Id,
                    definition.DisplayName));
            if (behaviors.MarketBehavior != null)
                entity.AddBehavior(new MarketBehavior(
                    behaviors.MarketBehavior.Title,
                    behaviors.MarketBehavior.Commodities
                        .Select(marketCommodity => new MarketCommodity(
                            commodities[marketCommodity.CommodityName],
                            marketCommodity.TargetAmount,
                            marketCommodity.MaxAmountPercentage,
                            marketCommodity.MinAmountPercentage,
                            marketCommodity.Consumption,
                            marketCommodity.Production,
                            marketCommodity.NormalPriceCoefficient))
                        .ToArray()));
            return entity;
        }
    }
}
