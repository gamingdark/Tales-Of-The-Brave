using TalesOfVoyages.Simulation.Chronicle;
using TalesOfVoyages.Simulation.Movement;
using TalesOfVoyages.Simulation.Time;
using TalesOfVoyages.Simulation.World;
using TalesOfVoyages.Simulation.Entities;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfVoyages.Simulation.Core
{
    public sealed class GameContext
    {
        private readonly string playerShipId;

        public const string PlayerShipId = "player_ship";
        public TimeManager Time { get; }
        public WorldGraph World { get; }
        public MovementManager Movement { get; }
        public Chronicler Chronicler { get; }
        public Transport PlayerShip => Movement.GetTransport(playerShipId);
        public IReadOnlyList<Entity> Entities { get; }

        public GameContext(
            TimeManager time,
            WorldGraph world,
            MovementManager movement,
            Chronicler chronicler,
            string playerShipId = PlayerShipId,
            IReadOnlyList<Entity> entities = null)
        {
            Time = time;
            World = world;
            Movement = movement;
            Chronicler = chronicler;
            this.playerShipId = playerShipId;
            Entities = entities ?? new List<Entity>();
        }

        public Entity GetPortAtNode(string nodeId) => Entities.Single(entity =>
            entity.HasBehavior<PortBehavior>() &&
            entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == nodeId);

        public Entity GetPendingInteractionEntity()
        {
            var travel = PlayerShip.Travel;
            if (!travel.IsApproachingNode(Time.DayProgress)) return null;
            return Entities.SingleOrDefault(entity =>
                entity.Actions.Count > 0 &&
                entity.HasBehavior<WorldEntityBehavior>() &&
                entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == travel.DestinationNodeId);
        }

        public void Tick(float realSeconds)
        {
            var travel = PlayerShip.Travel;
            if (travel.IsTravelling && travel.ArrivalDayFraction >= 0f)
            {
                var destination = Entities.SingleOrDefault(entity =>
                    entity.Actions.Count > 0 &&
                    entity.HasBehavior<WorldEntityBehavior>() &&
                    entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == travel.DestinationNodeId);
                if (destination != null)
                {
                    Time.TickUntilDayProgress(realSeconds, 1f);
                    return;
                }
            }
            Time.Tick(realSeconds);
        }

        public void ProcessDay(GameDate date)
        {
            Movement.ProcessDay();
        }
    }
}
