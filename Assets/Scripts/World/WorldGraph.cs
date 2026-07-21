using System;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfVoyages.Simulation.World
{
    public sealed class WorldGraph
    {
        private readonly Dictionary<string, WorldNode> nodes = new Dictionary<string, WorldNode>();
        private readonly Dictionary<string, WorldEdge> edges = new Dictionary<string, WorldEdge>();
        public IReadOnlyCollection<WorldNode> Nodes => nodes.Values;
        public IReadOnlyCollection<WorldEdge> Edges => edges.Values;

        public void AddNode(WorldNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            nodes.Add(node.Id, node);
        }

        public void AddEdge(WorldEdge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            if (!nodes.ContainsKey(edge.NodeAId) || !nodes.ContainsKey(edge.NodeBId)) throw new InvalidOperationException("Both edge nodes must exist first.");
            edges.Add(edge.Id, edge);
        }

        public WorldNode GetNode(string id) => nodes.TryGetValue(id, out var node) ? node : throw new KeyNotFoundException($"Unknown node '{id}'.");
        public WorldEdge GetEdge(string id) => edges.TryGetValue(id, out var edge) ? edge : throw new KeyNotFoundException($"Unknown edge '{id}'.");
        public IEnumerable<WorldNode> GetNeighbors(string nodeId) => edges.Values.Where(e => e.Connects(nodeId)).Select(e => GetNode(e.Other(nodeId)));
        public WorldEdge GetConnectingEdge(string a, string b) => edges.Values.FirstOrDefault(e => e.Connects(a) && e.Connects(b));

        public IReadOnlyList<string> FindRoute(string startId, string destinationId)
        {
            GetNode(startId); GetNode(destinationId);
            var queue = new Queue<string>();
            var previous = new Dictionary<string, string>();
            queue.Enqueue(startId); previous[startId] = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == destinationId) break;
                foreach (var neighbor in GetNeighbors(current))
                    if (!previous.ContainsKey(neighbor.Id)) { previous[neighbor.Id] = current; queue.Enqueue(neighbor.Id); }
            }
            if (!previous.ContainsKey(destinationId)) throw new InvalidOperationException("No route exists.");
            var result = new List<string>();
            for (var at = destinationId; at != null; at = previous[at]) result.Add(at);
            result.Reverse();
            return result;
        }
    }
}
