using System.Linq;
using TalesOfVoyages.Simulation.Chronicle;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.Rulesets;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;
using TalesOfVoyages.Simulation.Entities;
using System.Collections.Generic;

namespace TalesOfVoyages.Simulation.Core
{
    public static class MvpGameFactory
    {
        public static GameContext Create()
        {
            return Create(MvpWorldDefinition.CreateDefault());
        }

        public static GameContext Create(MvpWorldDefinition definition)
        {
            MvpWorldDefinitionValidator.Validate(definition);

            var timeDefinition = definition.TimeSystem;
            var time = new TimeManager(
                timeDefinition.SecondsPerDay,
                daysPerMonth: timeDefinition.DaysPerMonth,
                hoursPerDay: timeDefinition.HoursPerDay,
                dayStartHourOffset: timeDefinition.DayStartHourOffset,
                allowedSpeeds: timeDefinition.AllowedSpeeds);
            var world = new WorldGraph();

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
                entities.Add(CreateEntity(entityDefinition));

            var playerEntity = entities.Single(entity => entity.HasBehavior<PlayerControlledBehavior>());
            var movement = new MovementManager(world);
            movement.Register(new Transport(playerEntity));

            var chronicler = new Chronicler();
            var context = new GameContext(time, world, movement, chronicler, playerEntity.Id, entities);
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

        private static Entity CreateEntity(EntityDefinition definition)
        {
            var entity = new Entity(definition.Id, definition.DisplayName);
            var behaviors = definition.Behaviors;
            if (behaviors.PlayerControlledBehavior != null)
                entity.AddBehavior(new PlayerControlledBehavior(behaviors.PlayerControlledBehavior.SpeedPerDay));
            if (behaviors.DrawableBehavior != null)
                entity.AddBehavior(new DrawableBehavior(behaviors.DrawableBehavior.MapIconSprite));
            if (behaviors.WorldEntityBehavior != null)
                entity.AddBehavior(new WorldEntityBehavior(behaviors.WorldEntityBehavior.StartingNodeId));
            if (behaviors.PortBehavior != null)
                entity.AddBehavior(new PortBehavior(behaviors.PortBehavior.PortViewSprite));
            return entity;
        }
    }
}
