using System;
using System.Collections.Generic;

namespace TalesOfVoyages.Simulation.Movement
{
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
        public List<string> RemainingRoute { get; } = new List<string>();
        public bool IsTravelling => CurrentEdgeId != null;
        public bool HasPlannedAction => PlannedDestinationNodeId != null;

        public float GetVisualEdgeProgress(float dayProgress)
        {
            if (!HasActiveDaySegment) return EdgeProgress;
            return DayStartEdgeProgress + (DayEndEdgeProgress - DayStartEdgeProgress) * Math.Max(0f, Math.Min(1f, dayProgress));
        }

        public bool IsEnteringPort(float dayProgress) =>
            IsTravelling &&
            ArrivalDayFraction >= 0f &&
            dayProgress >= ArrivalDayFraction;
    }
}
