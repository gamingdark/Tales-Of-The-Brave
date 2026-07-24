using System;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfTheBrave.Simulation.World
{
    public sealed class WorldEdge
    {
        public string Id { get; }
        public string NodeAId { get; }
        public string NodeBId { get; }
        public float Distance { get; }
        public IReadOnlyList<WorldMapPoint> MapWaypoints { get; }

        public WorldEdge(
            string id,
            string nodeAId,
            string nodeBId,
            float distance,
            IEnumerable<WorldMapPoint> mapWaypoints = null)
        {
            if (distance <= 0f) throw new ArgumentOutOfRangeException(nameof(distance));
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NodeAId = nodeAId ?? throw new ArgumentNullException(nameof(nodeAId));
            NodeBId = nodeBId ?? throw new ArgumentNullException(nameof(nodeBId));
            Distance = distance;
            MapWaypoints = (mapWaypoints ?? Enumerable.Empty<WorldMapPoint>()).ToArray();
        }

        public bool Connects(string nodeId) => NodeAId == nodeId || NodeBId == nodeId;
        public string Other(string nodeId) => nodeId == NodeAId ? NodeBId : nodeId == NodeBId ? NodeAId : throw new ArgumentException("Node is not on this edge.", nameof(nodeId));
    }
}
