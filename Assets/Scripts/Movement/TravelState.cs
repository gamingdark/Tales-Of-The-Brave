using System;
using System.Collections.Generic;

namespace TalesOfTheBrave.Simulation.Movement
{
    public enum TravelStatus
    {
        AtNode,
        Travelling,
        InsideLocation
    }

    public sealed class DayTravelSegment
    {
        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public float StartEdgeProgress { get; }
        public float EndEdgeProgress { get; }
        public float StartDayFraction { get; }
        public float EndDayFraction { get; }
        public bool ReachesNode => EndEdgeProgress >= 1f;

        public DayTravelSegment(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            float startEdgeProgress,
            float endEdgeProgress,
            float startDayFraction,
            float endDayFraction)
        {
            EdgeId = edgeId;
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            StartEdgeProgress = startEdgeProgress;
            EndEdgeProgress = endEdgeProgress;
            StartDayFraction = startDayFraction;
            EndDayFraction = endDayFraction;
        }
    }

    [Serializable]
    public sealed class TravelState
    {
        public string CurrentNodeId { get; internal set; }
        public string DestinationNodeId { get; internal set; }
        public string PlannedDestinationNodeId { get; internal set; }
        public string CurrentEdgeId { get; internal set; }
        public float EdgeProgress { get; internal set; }
        public float DayStartEdgeProgress { get; internal set; }
        public float DayEndEdgeProgress { get; internal set; }
        public float ArrivalDayFraction { get; internal set; } = -1f;
        public bool HasActiveDaySegment { get; internal set; }
        public string InsideLocationEntityId { get; internal set; }
        public List<string> RemainingRoute { get; } = new List<string>();
        public List<DayTravelSegment> DaySegments { get; } = new List<DayTravelSegment>();
        public bool IsTravelling => CurrentEdgeId != null;
        public bool IsInsideLocation => InsideLocationEntityId != null;
        public TravelStatus Status => IsInsideLocation
            ? TravelStatus.InsideLocation
            : IsTravelling ? TravelStatus.Travelling : TravelStatus.AtNode;
        public bool HasPlannedAction => PlannedDestinationNodeId != null;
        public string NextNodeId => RemainingRoute.Count > 0 ? RemainingRoute[0] : null;

        public float GetVisualEdgeProgress(float dayProgress)
        {
            var segment = GetVisualSegment(dayProgress);
            if (segment == null) return EdgeProgress;
            var duration = segment.EndDayFraction - segment.StartDayFraction;
            var localProgress = duration <= 0f
                ? 1f
                : (Math.Max(segment.StartDayFraction, Math.Min(segment.EndDayFraction, dayProgress)) -
                   segment.StartDayFraction) / duration;
            return segment.StartEdgeProgress +
                   (segment.EndEdgeProgress - segment.StartEdgeProgress) * localProgress;
        }

        public DayTravelSegment GetVisualSegment(float dayProgress)
        {
            if (DaySegments.Count == 0) return null;
            foreach (var segment in DaySegments)
                if (dayProgress <= segment.EndDayFraction) return segment;
            return DaySegments[DaySegments.Count - 1];
        }

        public string GetNextNodeId(float dayProgress)
        {
            var segment = GetVisualSegment(dayProgress);
            return segment?.ToNodeId ?? NextNodeId;
        }

        public string GetReachedNodeId(float dayProgress)
        {
            string reached = null;
            foreach (var segment in DaySegments)
            {
                if (!segment.ReachesNode || dayProgress < segment.EndDayFraction) break;
                reached = segment.ToNodeId;
            }
            return reached;
        }

        public bool IsApproachingNode(float dayProgress) =>
            IsTravelling &&
            ArrivalDayFraction >= 0f &&
            dayProgress >= ArrivalDayFraction;

        public bool IsEnteringLocation(float dayProgress) => IsApproachingNode(dayProgress);
    }
}
