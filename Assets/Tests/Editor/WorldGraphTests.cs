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

    [Test]
    public void FindRouteUsesShortestTotalEdgeDistance()
    {
        var graph = new WorldGraph();
        graph.AddNode(new WorldNode("a", "A", WorldNodeType.Sea, 0f, 0f));
        graph.AddNode(new WorldNode("b", "B", WorldNodeType.Sea, 0f, 0f));
        graph.AddNode(new WorldNode("c", "C", WorldNodeType.Sea, 0f, 0f));
        graph.AddNode(new WorldNode("d", "D", WorldNodeType.Sea, 0f, 0f));
        graph.AddEdge(new WorldEdge("a_d_long", "a", "d", 100f));
        graph.AddEdge(new WorldEdge("a_b", "a", "b", 20f));
        graph.AddEdge(new WorldEdge("b_c", "b", "c", 20f));
        graph.AddEdge(new WorldEdge("c_d", "c", "d", 20f));

        var route = graph.FindRoute("a", "d");

        Assert.That(route, Is.EqualTo(new[] { "a", "b", "c", "d" }));
        Assert.That(graph.GetRouteDistance(route), Is.EqualTo(60f));
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
