using System;

namespace TalesOfVoyages.Simulation.World
{
    public enum WorldNodeType { Port, Sea, HiddenLocation }

    public sealed class WorldNode
    {
        public string Id { get; }
        public string DisplayName { get; }
        public WorldNodeType Type { get; }
        public float MapX { get; }
        public float MapY { get; }
        public bool IsDiscovered { get; private set; }

        public WorldNode(string id, string displayName, WorldNodeType type, float mapX, float mapY,
            bool discovered = true)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("A node ID is required.", nameof(id)) : id;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Type = type;
            MapX = mapX;
            MapY = mapY;
            IsDiscovered = discovered;
        }

        public void Discover() => IsDiscovered = true;
    }
}
