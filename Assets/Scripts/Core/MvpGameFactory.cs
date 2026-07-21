using TalesOfVoyages.Simulation.Chronicle;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;

namespace TalesOfVoyages.Simulation.Core
{
    public static class MvpGameFactory
    {
        public static GameContext Create()
        {
            var time = new TimeManager(7.5f);
            var world = new WorldGraph();
            world.AddNode(new WorldNode("port_klaipeda", "Klaipėda", WorldNodeType.Port, 0.62f, 0.2f));
            world.AddNode(new WorldNode("port_riga", "Riga", WorldNodeType.Port, 0.73f, 0.33f));
            world.AddNode(new WorldNode("port_helsinki", "Helsinki", WorldNodeType.Port, 0.70f, 0.63f));
            world.AddEdge(new WorldEdge(
                "route_klaipeda_riga", "port_klaipeda", "port_riga", 75f,
                new[] { 
                    new WorldMapPoint(0.59f, 0.21f),
                    new WorldMapPoint(0.59f, 0.32f),
                    new WorldMapPoint(0.63f, 0.39f),
                    new WorldMapPoint(0.68f, 0.42f),
                    new WorldMapPoint(0.73f, 0.34f),
                }));
            world.AddEdge(new WorldEdge(
                "route_riga_helsinki", "port_riga", "port_helsinki", 90f,
                new[] {
                    new WorldMapPoint(0.73f, 0.34f),
                    new WorldMapPoint(0.68f, 0.42f),
                    new WorldMapPoint(0.63f, 0.39f),
                    new WorldMapPoint(0.61f, 0.47f),
                    new WorldMapPoint(0.64f, 0.56f),
                    new WorldMapPoint(0.69f, 0.59f),
                }));
            world.AddEdge(new WorldEdge(
                "route_helsinki_klaipeda", "port_helsinki", "port_klaipeda", 110f,
                new[] {
                    new WorldMapPoint(0.69f, 0.59f),
                    new WorldMapPoint(0.64f, 0.56f),
                    new WorldMapPoint(0.61f, 0.47f),
                    new WorldMapPoint(0.59f, 0.32f),
                    new WorldMapPoint(0.59f, 0.21f),
                }));
            var movement = new MovementManager(world);
            movement.Register(new Transport(GameContext.PlayerShipId, "The Unsinkable MVP", 25f, "port_klaipeda"));
            var chronicler = new Chronicler();
            var context = new GameContext(time, world, movement, chronicler);
            time.DayAdvanced += context.ProcessDay;
            movement.VoyageStarted += (ship, from, to) => chronicler.Record(time.CurrentDate, "VoyageStarted", $"We left {world.GetNode(from).DisplayName} for {world.GetNode(to).DisplayName} aboard {ship.DisplayName}.");
            movement.Arrived += (ship, at) => chronicler.Record(time.CurrentDate, "VoyageCompleted", $"We arrived safely at {world.GetNode(at).DisplayName}.");
            chronicler.Record(time.CurrentDate, "GameStarted", "Our voyage begins in Klaipėda.");
            return context;
        }
    }
}
