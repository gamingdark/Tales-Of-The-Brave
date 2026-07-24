using TalesOfTheBrave.Simulation.Chronicle;
using TalesOfTheBrave.Simulation.Movement;
using TalesOfTheBrave.Simulation.Time;
using TalesOfTheBrave.Simulation.World;
using TalesOfTheBrave.Simulation.Entities;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfTheBrave.Simulation.Core
{
    public sealed class GameContext
    {
        private readonly string playerShipId;
        private TimeSpeed speedBeforeEnteringLocation;

        public const string PlayerShipId = "player_ship";
        public TimeManager Time { get; }
        public WorldGraph World { get; }
        public MovementManager Movement { get; }
        public Chronicler Chronicler { get; }
        public Transport PlayerShip => Movement.GetTransport(playerShipId);
        public IReadOnlyList<Entity> Entities { get; }
        public IReadOnlyDictionary<string, Commodity> Commodities { get; }

        public GameContext(
            TimeManager time,
            WorldGraph world,
            MovementManager movement,
            Chronicler chronicler,
            string playerShipId = PlayerShipId,
            IReadOnlyList<Entity> entities = null,
            IReadOnlyDictionary<string, Commodity> commodities = null)
        {
            Time = time;
            World = world;
            Movement = movement;
            Chronicler = chronicler;
            this.playerShipId = playerShipId;
            Entities = entities ?? new List<Entity>();
            Commodities = commodities ?? new Dictionary<string, Commodity>();
        }

        public Entity GetLocationAtNode(string nodeId) => Entities.SingleOrDefault(entity =>
            entity.HasBehavior<LocationBehavior>() &&
            entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == nodeId);

        public Entity GetInsideLocationEntity() =>
            PlayerShip.Travel.IsInsideLocation
                ? Entities.Single(entity => entity.Id == PlayerShip.Travel.InsideLocationEntityId)
                : null;

        public void EnterLocation(string locationEntityId)
        {
            var travel = PlayerShip.Travel;
            if (travel.IsTravelling || travel.IsInsideLocation)
                throw new System.InvalidOperationException("The ship cannot enter a location in its current state.");
            var location = Entities.SingleOrDefault(entity =>
                entity.Id == locationEntityId &&
                entity.HasBehavior<LocationBehavior>() &&
                entity.HasBehavior<WorldEntityBehavior>() &&
                entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == travel.CurrentNodeId);
            if (location == null)
                throw new System.InvalidOperationException("The ship is not at that location.");

            speedBeforeEnteringLocation = Time.Speed;
            travel.InsideLocationEntityId = location.Id;
            Time.SetSpeed(TimeSpeed.Paused);
        }

        public void ExitLocation()
        {
            var travel = PlayerShip.Travel;
            if (!travel.IsInsideLocation)
                throw new System.InvalidOperationException("The ship is not inside a location.");
            travel.InsideLocationEntityId = null;
            Time.SetSpeed(speedBeforeEnteringLocation);
        }

        public Entity GetPendingInteractionEntity()
        {
            var travel = PlayerShip.Travel;
            if (!travel.IsApproachingNode(Time.DayProgress)) return null;
            return GetInteractionEntityAtNode(travel.GetReachedNodeId(Time.DayProgress));
        }

        private Entity GetInteractionEntityAtNode(string nodeId)
        {
            if (nodeId == null) return null;
            return Entities.FirstOrDefault(entity =>
                entity.Actions.Count > 0 &&
                entity.HasBehavior<WorldEntityBehavior>() &&
                entity.GetBehavior<WorldEntityBehavior>().StartingNodeId == nodeId);
        }

        public void Tick(float realSeconds)
        {
            var travel = PlayerShip.Travel;
            if (travel.IsInsideLocation) return;
            var finalSegment = travel.DaySegments.LastOrDefault();
            if (travel.IsTravelling &&
                finalSegment != null &&
                finalSegment.ReachesNode &&
                GetInteractionEntityAtNode(finalSegment.ToNodeId) != null)
            {
                Time.TickUntilDayProgress(realSeconds, 1f);
                return;
            }
            Time.Tick(realSeconds);
        }

        public void ProcessDay(GameDate date)
        {
            Movement.ProcessDay();
        }
    }
}
