using System;
using System.Collections.Generic;
using TalesOfVoyages.Simulation.World;

namespace TalesOfVoyages.Simulation.Movement
{
    public sealed class MovementManager
    {
        private readonly WorldGraph world;
        private readonly Func<string, bool> requiresInteraction;
        private readonly Dictionary<string, Transport> transports = new Dictionary<string, Transport>();
        public event Action<Transport, string, string> VoyageStarted;
        public event Action<Transport, string> Arrived;

        public MovementManager(WorldGraph world, Func<string, bool> requiresInteraction = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.requiresInteraction = requiresInteraction ?? (_ => false);
        }

        public void Register(Transport transport)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            world.GetNode(transport.Travel.CurrentNodeId);
            transports.Add(transport.Id, transport);
        }

        public Transport GetTransport(string id) => transports.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Unknown transport '{id}'.");

        public void PlanDestination(string transportId, string destinationNodeId)
        {
            var transport = GetTransport(transportId);
            var state = transport.Travel;
            if (state.IsTravelling) throw new InvalidOperationException("The ship is already travelling.");
            if (state.CurrentNodeId == destinationNodeId) throw new InvalidOperationException("The ship is already there.");
            world.FindRoute(state.CurrentNodeId, destinationNodeId);
            state.PlannedDestinationNodeId = destinationNodeId;
        }

        public void CancelPlannedDestination(string transportId)
        {
            var state = GetTransport(transportId).Travel;
            if (state.IsTravelling) throw new InvalidOperationException("A voyage already underway cannot be cancelled here.");
            state.PlannedDestinationNodeId = null;
        }

        // Kept as the public command name expected by future callers; departure occurs on the next daily pulse.
        public void SetDestination(string transportId, string destinationNodeId) => PlanDestination(transportId, destinationNodeId);

        private void StartPlannedVoyage(Transport transport)
        {
            var state = transport.Travel;
            var destinationNodeId = state.PlannedDestinationNodeId;
            if (destinationNodeId == null) return;
            var path = world.FindRoute(state.CurrentNodeId, destinationNodeId);
            state.PlannedDestinationNodeId = null;
            state.DestinationNodeId = destinationNodeId;
            state.RemainingRoute.Clear();
            for (var i = 1; i < path.Count; i++) state.RemainingRoute.Add(path[i]);
            BeginNextEdge(transport);
            VoyageStarted?.Invoke(transport, path[0], destinationNodeId);
        }

        public void ProcessDay()
        {
            foreach (var transport in transports.Values)
            {
                FinalizePreviousDay(transport);
                if (!transport.Travel.IsTravelling) StartPlannedVoyage(transport);
                if (transport.Travel.IsTravelling) PrepareDaySegment(transport);
            }
        }

        private void FinalizePreviousDay(Transport transport)
        {
            var state = transport.Travel;
            if (!state.HasActiveDaySegment) return;

            foreach (var segment in state.DaySegments)
            {
                state.EdgeProgress = segment.EndEdgeProgress;
                if (segment.ReachesNode) CompleteEdge(transport);
            }

            state.HasActiveDaySegment = false;
            state.ArrivalDayFraction = -1f;
            state.DaySegments.Clear();
        }

        private void PrepareDaySegment(Transport transport)
        {
            var state = transport.Travel;
            state.DaySegments.Clear();
            state.DayStartEdgeProgress = state.EdgeProgress;
            var distanceBudget = transport.SpeedPerDay;
            var elapsedDayFraction = 0f;
            var edgeId = state.CurrentEdgeId;
            var fromNodeId = state.CurrentNodeId;
            var routeIndex = 0;
            var edgeProgress = state.EdgeProgress;

            while (edgeId != null && distanceBudget > 0f && routeIndex < state.RemainingRoute.Count)
            {
                var edge = world.GetEdge(edgeId);
                var toNodeId = state.RemainingRoute[routeIndex];
                var distanceRemaining = edge.Distance * (1f - edgeProgress);
                var distanceTravelled = Math.Min(distanceBudget, distanceRemaining);
                var endProgress = Math.Min(1f, edgeProgress + distanceTravelled / edge.Distance);
                var endDayFraction = Math.Min(
                    1f,
                    elapsedDayFraction + distanceTravelled / transport.SpeedPerDay);
                state.DaySegments.Add(new DayTravelSegment(
                    edgeId,
                    fromNodeId,
                    toNodeId,
                    edgeProgress,
                    endProgress,
                    elapsedDayFraction,
                    endDayFraction));
                distanceBudget -= distanceTravelled;
                elapsedDayFraction = endDayFraction;

                if (endProgress < 1f) break;
                var finalDestination = routeIndex == state.RemainingRoute.Count - 1;
                if (finalDestination || requiresInteraction(toNodeId)) break;

                fromNodeId = toNodeId;
                routeIndex++;
                var nextNodeId = state.RemainingRoute[routeIndex];
                edgeId = world.GetConnectingEdge(fromNodeId, nextNodeId)?.Id
                    ?? throw new InvalidOperationException("Route edge is missing.");
                edgeProgress = 0f;
            }

            var finalSegment = state.DaySegments[state.DaySegments.Count - 1];
            state.DayEndEdgeProgress = finalSegment.EndEdgeProgress;
            state.ArrivalDayFraction = finalSegment.ReachesNode
                ? finalSegment.EndDayFraction
                : -1f;
            state.HasActiveDaySegment = true;
        }

        private void BeginNextEdge(Transport transport)
        {
            var state = transport.Travel;
            var nextNode = state.RemainingRoute[0];
            var edge = world.GetConnectingEdge(state.CurrentNodeId, nextNode) ?? throw new InvalidOperationException("Route edge is missing.");
            state.CurrentEdgeId = edge.Id;
            state.EdgeProgress = 0f;
            state.DayStartEdgeProgress = 0f;
            state.DayEndEdgeProgress = 0f;
        }

        private void CompleteEdge(Transport transport)
        {
            var state = transport.Travel;
            state.CurrentNodeId = state.RemainingRoute[0];
            state.RemainingRoute.RemoveAt(0);
            state.CurrentEdgeId = null;
            state.EdgeProgress = 0f;
            if (state.RemainingRoute.Count > 0) BeginNextEdge(transport);
            else
            {
                state.DestinationNodeId = null;
                Arrived?.Invoke(transport, state.CurrentNodeId);
            }
        }
    }
}
