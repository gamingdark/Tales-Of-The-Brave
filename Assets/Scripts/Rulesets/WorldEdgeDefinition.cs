using System;
using System.Collections.Generic;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class WorldEdgeDefinition
    {
        public string Id;
        public string NodeAId;
        public string NodeBId;
        public float Distance;
        public List<RouteMapPointDefinition> MapWaypoints = new List<RouteMapPointDefinition>();

        public WorldEdgeDefinition() { }

        public WorldEdgeDefinition(
            string id,
            string nodeAId,
            string nodeBId,
            float distance,
            IEnumerable<RouteMapPointDefinition> mapWaypoints = null)
        {
            Id = id;
            NodeAId = nodeAId;
            NodeBId = nodeBId;
            Distance = distance;
            if (mapWaypoints != null) MapWaypoints.AddRange(mapWaypoints);
        }
    }
}
