using System;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfTheBrave.Simulation.World
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

        public float GetRouteDistance(IReadOnlyList<string> route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            var distance = 0f;
            for (var i = 1; i < route.Count; i++)
            {
                var edge = GetConnectingEdge(route[i - 1], route[i])
                    ?? throw new InvalidOperationException(
                        $"No edge connects route nodes '{route[i - 1]}' and '{route[i]}'.");
                distance += edge.Distance;
            }
            return distance;
        }

        public IReadOnlyList<string> FindRoute(string startId, string destinationId)
        {
            GetNode(startId); GetNode(destinationId);
            var unvisited = new HashSet<string>(nodes.Keys, StringComparer.Ordinal);
            var distances = nodes.Keys.ToDictionary(id => id, _ => float.PositiveInfinity);
            var previous = new Dictionary<string, string>();
            distances[startId] = 0f;
            previous[startId] = null;

            while (unvisited.Count > 0)
            {
                var current = unvisited
                    .OrderBy(id => distances[id])
                    .ThenBy(id => id, StringComparer.Ordinal)
                    .First();
                if (float.IsPositiveInfinity(distances[current])) break;
                if (current == destinationId) break;
                unvisited.Remove(current);

                foreach (var edge in edges.Values.Where(edge => edge.Connects(current)))
                {
                    var neighborId = edge.Other(current);
                    if (!unvisited.Contains(neighborId)) continue;
                    var candidateDistance = distances[current] + edge.Distance;
                    if (candidateDistance >= distances[neighborId]) continue;
                    distances[neighborId] = candidateDistance;
                    previous[neighborId] = current;
                }
            }

            if (!previous.ContainsKey(destinationId)) throw new InvalidOperationException("No route exists.");
            var result = new List<string>();
            for (var at = destinationId; at != null; at = previous[at]) result.Add(at);
            result.Reverse();
            return result;
        }
    }
}
