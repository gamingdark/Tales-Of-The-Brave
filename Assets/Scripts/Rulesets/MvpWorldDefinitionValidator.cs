using System;
using System.Collections.Generic;
using TalesOfVoyages.Graphics;
using TalesOfVoyages.Simulation.Time;

namespace TalesOfVoyages.Simulation.Rulesets
{
    public static class MvpWorldDefinitionValidator
    {
        public static void Validate(MvpWorldDefinition definition, ISpriteNameLookup spriteLookup = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Nodes == null) throw new InvalidOperationException("World node definitions are required.");
            if (definition.Edges == null) throw new InvalidOperationException("World edge definitions are required.");
            if (definition.Entities == null) throw new InvalidOperationException("Entity definitions are required.");
            ValidateTimeSystem(definition.TimeSystem);

            var allIds = new HashSet<string>(StringComparer.Ordinal);
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in definition.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                    throw new InvalidOperationException("Every world node requires an ID.");
                if (!nodeIds.Add(node.Id) || !allIds.Add(node.Id))
                    throw new InvalidOperationException($"Duplicate definition ID '{node.Id}'.");
            }

            foreach (var edge in definition.Edges)
            {
                if (edge == null || string.IsNullOrWhiteSpace(edge.Id))
                    throw new InvalidOperationException("Every world edge requires an ID.");
                if (!allIds.Add(edge.Id))
                    throw new InvalidOperationException($"Duplicate definition ID '{edge.Id}'.");
                if (edge.Distance <= 0f)
                    throw new InvalidOperationException($"Edge '{edge.Id}' must have a positive distance.");
                if (!nodeIds.Contains(edge.NodeAId) || !nodeIds.Contains(edge.NodeBId))
                    throw new InvalidOperationException($"Edge '{edge.Id}' references a missing endpoint node.");
            }

            var playerControlledCount = 0;
            foreach (var entity in definition.Entities)
            {
                if (entity == null || string.IsNullOrWhiteSpace(entity.Id))
                    throw new InvalidOperationException("Every entity requires an ID.");
                if (!allIds.Add(entity.Id))
                    throw new InvalidOperationException($"Duplicate definition ID '{entity.Id}'.");
                if (entity.Behaviors == null)
                    throw new InvalidOperationException($"Entity '{entity.Id}' requires a behaviors definition.");

                var behaviors = entity.Behaviors;
                if (behaviors.PlayerControlledBehavior != null)
                {
                    playerControlledCount++;
                    if (behaviors.PlayerControlledBehavior.SpeedPerDay <= 0f)
                        throw new InvalidOperationException($"Player-controlled entity '{entity.Id}' must have positive speed.");
                }
                if (behaviors.WorldEntityBehavior == null)
                    throw new InvalidOperationException($"Map entity '{entity.Id}' requires a world entity behavior.");
                if (!nodeIds.Contains(behaviors.WorldEntityBehavior.StartingNodeId))
                    throw new InvalidOperationException(
                        $"Entity '{entity.Id}' starting node '{behaviors.WorldEntityBehavior.StartingNodeId}' does not exist.");
                if (behaviors.DrawableBehavior == null)
                    throw new InvalidOperationException($"Map entity '{entity.Id}' requires a drawable behavior.");
                if (spriteLookup != null && !spriteLookup.ContainsSprite(behaviors.DrawableBehavior.MapIconSprite))
                    throw new InvalidOperationException(
                        $"Entity '{entity.Id}' references missing map icon sprite '{behaviors.DrawableBehavior.MapIconSprite}'.");
                if (behaviors.PortBehavior != null &&
                    (spriteLookup == null ? string.IsNullOrWhiteSpace(behaviors.PortBehavior.PortViewSprite)
                        : !spriteLookup.ContainsSprite(behaviors.PortBehavior.PortViewSprite)))
                    throw new InvalidOperationException(
                        $"Port entity '{entity.Id}' references missing port view sprite '{behaviors.PortBehavior.PortViewSprite}'.");
            }
            if (playerControlledCount != 1)
                throw new InvalidOperationException("Exactly one player-controlled entity is required.");
        }

        private static void ValidateTimeSystem(TimeSystemDefinition time)
        {
            if (time == null) throw new InvalidOperationException("A time system definition is required.");
            if (time.SecondsPerDay <= 0f) throw new InvalidOperationException("Time system day length must be positive.");
            if (time.DaysPerMonth <= 0) throw new InvalidOperationException("Time system days per month must be positive.");
            if (time.HoursPerDay <= 0) throw new InvalidOperationException("Time system hours per day must be positive.");
            if (time.DayStartHourOffset < 0 || time.DayStartHourOffset >= time.HoursPerDay)
                throw new InvalidOperationException("Time system day-start offset must be within the configured day.");
            if (time.AllowedSpeeds == null || time.AllowedSpeeds.Count == 0)
                throw new InvalidOperationException("Time system requires at least one allowed running speed.");

            var speeds = new HashSet<TimeSpeed>();
            foreach (var speed in time.AllowedSpeeds)
            {
                if (!Enum.IsDefined(typeof(TimeSpeed), speed) || speed == TimeSpeed.Paused || speed == TimeSpeed.Developer)
                    throw new InvalidOperationException($"Time speed '{speed}' cannot be configured as a running speed.");
                if (!speeds.Add(speed))
                    throw new InvalidOperationException($"Time speed '{speed}' is configured more than once.");
            }
        }
    }
}
