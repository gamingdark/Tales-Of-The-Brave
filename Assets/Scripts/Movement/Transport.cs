using System;
using TalesOfVoyages.Simulation.Entities;

namespace TalesOfVoyages.Simulation.Movement
{
    public sealed class Transport
    {
        public string Id { get; }
        public string DisplayName { get; }
        private readonly Entity entity;
        public float SpeedPerDay => entity.GetBehavior<TransportBehavior>().SpeedPerDay;
        public TravelState Travel { get; }
        public string MapIconSprite => entity.GetBehavior<DrawableBehavior>().MapIconSprite;
        public Entity Entity => entity;

        public Transport(string id, string displayName, float speedPerDay, string startingNodeId,
            string mapIconSprite = null)
            : this(CreateEntity(id, displayName, speedPerDay, startingNodeId, mapIconSprite))
        {
        }

        public Transport(Entity entity)
        {
            this.entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Id = entity.Id;
            DisplayName = entity.DisplayName;
            if (SpeedPerDay <= 0f) throw new ArgumentOutOfRangeException(nameof(entity), "Speed per day must be positive.");
            var startingNodeId = entity.GetBehavior<WorldEntityBehavior>().StartingNodeId;
            Travel = new TravelState { CurrentNodeId = startingNodeId };
        }

        private static Entity CreateEntity(
            string id, string displayName, float speedPerDay, string startingNodeId, string mapIconSprite)
        {
            var entity = new Entity(id, displayName);
            entity.AddBehavior(new PlayerControlledBehavior());
            entity.AddBehavior(new TransportBehavior(speedPerDay));
            entity.AddBehavior(new DrawableBehavior(mapIconSprite));
            entity.AddBehavior(new WorldEntityBehavior(startingNodeId));
            return entity;
        }
    }
}
