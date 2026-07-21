using TalesOfVoyages.Simulation.Chronicle;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;

namespace TalesOfVoyages.Simulation.Core
{
    public sealed class GameContext
    {
        public const string PlayerShipId = "ship_player";
        public TimeManager Time { get; }
        public WorldGraph World { get; }
        public MovementManager Movement { get; }
        public Chronicler Chronicler { get; }
        public Transport PlayerShip => Movement.GetTransport(PlayerShipId);

        public GameContext(TimeManager time, WorldGraph world, MovementManager movement, Chronicler chronicler)
        { Time = time; World = world; Movement = movement; Chronicler = chronicler; }

        public void ProcessDay(GameDate date)
        {
            Movement.ProcessDay();
        }
    }
}
