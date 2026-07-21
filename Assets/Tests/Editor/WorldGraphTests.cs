using System.Linq;
using NUnit.Framework;
using TalesOfVoyages.Simulation.World;

public sealed class WorldGraphTests
{
    [Test]
    public void AddedEdgeConnectsItsTwoNodes()
    {
        var graph = CreateTwoNodeGraph();

        Assert.That(graph.GetNeighbors("node_a").Select(node => node.Id), Is.EquivalentTo(new[] { "node_b" }));
        Assert.That(graph.GetNeighbors("node_b").Select(node => node.Id), Is.EquivalentTo(new[] { "node_a" }));
    }

    [Test]
    public void FindRouteReturnsPathThroughConnectedNodes()
    {
        var graph = CreateTwoNodeGraph();

        Assert.That(graph.FindRoute("node_a", "node_b"), Is.EqualTo(new[] { "node_a", "node_b" }));
    }

    private static WorldGraph CreateTwoNodeGraph()
    {
        var graph = new WorldGraph();
        graph.AddNode(new WorldNode("node_a", "A", WorldNodeType.Port, 0f, 0f));
        graph.AddNode(new WorldNode("node_b", "B", WorldNodeType.Port, 1f, 1f));
        graph.AddEdge(new WorldEdge("edge_ab", "node_a", "node_b", 10f));
        return graph;
    }
}
