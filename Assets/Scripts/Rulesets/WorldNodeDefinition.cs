using System;
using TalesOfTheBrave.Simulation.World;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class WorldNodeDefinition
    {
        public string Id;
        public string DisplayName;
        public WorldNodeType Type;
        public float MapX;
        public float MapY;
        public bool IsDiscovered = true;

        public WorldNodeDefinition() { }

        public WorldNodeDefinition(
            string id,
            string displayName,
            WorldNodeType type,
            float mapX,
            float mapY,
            bool isDiscovered = true)
        {
            Id = id;
            DisplayName = displayName;
            Type = type;
            MapX = mapX;
            MapY = mapY;
            IsDiscovered = isDiscovered;
        }
    }
}
