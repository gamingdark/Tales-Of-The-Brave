using System;

namespace TalesOfVoyages.Simulation.Movement
{
    public sealed class Transport
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float SpeedPerDay { get; }
        public TravelState Travel { get; }

        public Transport(string id, string displayName, float speedPerDay, string startingNodeId)
        {
            if (speedPerDay <= 0f) throw new ArgumentOutOfRangeException(nameof(speedPerDay));
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            SpeedPerDay = speedPerDay;
            Travel = new TravelState { CurrentNodeId = startingNodeId ?? throw new ArgumentNullException(nameof(startingNodeId)) };
        }
    }
}
