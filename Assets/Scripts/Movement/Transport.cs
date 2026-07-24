using System;
using System.Collections.Generic;
using System.Linq;
using TalesOfTheBrave.Simulation.Entities;

namespace TalesOfTheBrave.Simulation.Movement
{
    public sealed class Transport
    {
        public string Id { get; }
        public string DisplayName { get; }
        private readonly Entity entity;
        public float SpeedPerDay => entity.GetBehavior<TransportBehavior>().SpeedPerDay;
        public int MaxCargoAmount => entity.GetBehavior<TransportBehavior>().MaxCargoAmount;
        public int CurrentGold => entity.GetBehavior<TransportBehavior>().CurrentGold;
        public List<CargoCommodity> Wares { get; } = new List<CargoCommodity>();
        public List<CargoItemStack> Supplies { get; } = new List<CargoItemStack>();
        public List<CargoItemStack> Restricted { get; } = new List<CargoItemStack>();
        // Compatibility alias while callers migrate to the named cargo sections.
        public List<CargoCommodity> CurrentCargo => Wares;
        public int CurrentCargoAmount =>
            Wares.Sum(cargo => cargo.Amount) +
            Supplies.Sum(cargo => cargo.Amount) +
            Restricted.Sum(cargo => cargo.Amount);
        public TravelState Travel { get; }
        public string MapIconSprite => entity.GetBehavior<DrawableBehavior>().MapIconSprite;
        public Entity Entity => entity;

        public Transport(string id, string displayName, float speedPerDay, string startingNodeId,
            string mapIconSprite = null,
            int maxCargoAmount = 200,
            int currentGold = 1000)
            : this(CreateEntity(
                id, displayName, speedPerDay, startingNodeId, mapIconSprite,
                maxCargoAmount, currentGold))
        {
        }

        public int GetCargoAmount(Commodity commodity) =>
            Wares.FirstOrDefault(cargo => cargo.Commodity == commodity)?.Amount ?? 0;

        public void ChangeCargo(Commodity commodity, int amount)
        {
            var cargo = Wares.FirstOrDefault(entry => entry.Commodity == commodity);
            if (cargo == null)
            {
                if (amount < 0) throw new InvalidOperationException("Not enough cargo to sell.");
                if (amount == 0) return;
                cargo = new CargoCommodity(commodity, 0);
                Wares.Add(cargo);
            }
            if (cargo.Amount + amount < 0)
                throw new InvalidOperationException("Not enough cargo to sell.");
            if (CurrentCargoAmount + amount > MaxCargoAmount)
                throw new InvalidOperationException("Cargo capacity would be exceeded.");
            cargo.Amount += amount;
            if (cargo.Amount == 0) Wares.Remove(cargo);
        }

        public void ChangeGold(int amount)
        {
            var behavior = entity.GetBehavior<TransportBehavior>();
            if (behavior.CurrentGold + amount < 0)
                throw new InvalidOperationException("Not enough gold.");
            behavior.CurrentGold += amount;
        }

        public Transport(Entity entity)
        {
            this.entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Id = entity.Id;
            DisplayName = entity.DisplayName;
            if (SpeedPerDay <= 0f) throw new ArgumentOutOfRangeException(nameof(entity), "Speed per day must be positive.");
            if (MaxCargoAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(entity), "Maximum cargo amount must be positive.");
            var startingNodeId = entity.GetBehavior<WorldEntityBehavior>().StartingNodeId;
            Travel = new TravelState { CurrentNodeId = startingNodeId };
        }

        private static Entity CreateEntity(
            string id,
            string displayName,
            float speedPerDay,
            string startingNodeId,
            string mapIconSprite,
            int maxCargoAmount,
            int currentGold)
        {
            var entity = new Entity(id, displayName);
            entity.AddBehavior(new PlayerControlledBehavior());
            entity.AddBehavior(new TransportBehavior(
                speedPerDay, maxCargoAmount, currentGold));
            entity.AddBehavior(new DrawableBehavior(mapIconSprite));
            entity.AddBehavior(new WorldEntityBehavior(startingNodeId));
            return entity;
        }
    }
}
