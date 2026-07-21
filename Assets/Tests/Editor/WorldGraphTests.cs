using System.Linq;
using NUnit.Framework;
using TalesOfVoyages.Simulation.Core;

public sealed class WorldGraphTests
{
    [Test]
    public void InitialWorldContainsThreePortsConnectedAsATriangle()
    {
        var game = MvpGameFactory.Create();
        Assert.That(game.World.Nodes.Count, Is.EqualTo(3));
        Assert.That(game.World.Edges.Count, Is.EqualTo(3));
        Assert.That(game.World.GetEdge("route_klaipeda_riga").MapWaypoints.Count, Is.EqualTo(5));
        Assert.That(game.World.GetEdge("route_riga_helsinki").MapWaypoints.Count, Is.EqualTo(6));
        Assert.That(game.World.GetEdge("route_helsinki_klaipeda").MapWaypoints.Count, Is.EqualTo(5));
        Assert.That(game.World.Edges.SelectMany(edge => edge.MapWaypoints).Count(), Is.EqualTo(16));
        Assert.That(game.World.GetNeighbors("port_klaipeda").Select(node => node.Id),
            Is.EquivalentTo(new[] { "port_riga", "port_helsinki" }));
        Assert.That(game.World.GetNeighbors("port_riga").Select(node => node.Id),
            Is.EquivalentTo(new[] { "port_klaipeda", "port_helsinki" }));
        Assert.That(game.World.GetNeighbors("port_helsinki").Select(node => node.Id),
            Is.EquivalentTo(new[] { "port_riga", "port_klaipeda" }));
        Assert.That(game.PlayerShip.Travel.CurrentNodeId, Is.EqualTo("port_klaipeda"));
    }
}
