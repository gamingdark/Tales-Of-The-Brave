using System;
using System.Collections.Generic;
using System.Linq;
using TalesOfTheBrave.Simulation.Movement;

namespace TalesOfTheBrave.Simulation.Entities
{
    public interface IEntityBehavior { }
    public interface ILocationAction : IEntityBehavior
    {
        string Title { get; }
    }

    public sealed class PlayerControlledBehavior : IEntityBehavior { }

    public sealed class TransportBehavior : IEntityBehavior
    {
        public float SpeedPerDay { get; }
        public int MaxCargoAmount { get; }
        public int CurrentGold { get; internal set; }
        public TransportBehavior(
            float speedPerDay,
            int maxCargoAmount = 200,
            int currentGold = 1000)
        {
            SpeedPerDay = speedPerDay;
            MaxCargoAmount = maxCargoAmount;
            CurrentGold = currentGold;
        }
    }

    public sealed class DrawableBehavior : IEntityBehavior
    {
        public string MapIconSprite { get; }
        public DrawableBehavior(string mapIconSprite) => MapIconSprite = mapIconSprite;
    }

    public sealed class WorldEntityBehavior : IEntityBehavior
    {
        public string StartingNodeId { get; }
        public WorldEntityBehavior(string startingNodeId) => StartingNodeId = startingNodeId;
    }

    public sealed class LocationBehavior : IProvidesEntityActions
    {
        private readonly IEntityAction[] locationActions;
        public string LocationViewSprite { get; }
        public System.Collections.Generic.IReadOnlyList<IEntityAction> Actions => locationActions;
        public LocationBehavior(
            string locationViewSprite,
            string entityId,
            string displayName)
        {
            LocationViewSprite = locationViewSprite;
            locationActions = new IEntityAction[]
            {
                new EnterLocationAction(entityId),
                new GoIntoLocationAction(entityId, displayName)
            };
        }
    }

    public sealed class Commodity
    {
        public string Name { get; }
        public int DefaultPrice { get; }
        public string UnitName { get; }
        public string UnitAbbreviation { get; }

        public Commodity(string name, int defaultPrice, string unitName, string unitAbbreviation)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DefaultPrice = defaultPrice;
            UnitName = unitName ?? throw new ArgumentNullException(nameof(unitName));
            UnitAbbreviation = unitAbbreviation ?? throw new ArgumentNullException(nameof(unitAbbreviation));
        }
    }

    public sealed class MarketCommodity
    {
        public Commodity Commodity { get; }
        public int TargetAmount { get; }
        public float MaxAmountPercentage { get; }
        public float MinAmountPercentage { get; }
        public int Consumption { get; }
        public int Production { get; }
        public float NormalPriceCoefficient { get; }
        public int CurrentAmount { get; internal set; }
        public int CurrentPrice => (int)Math.Round(
            Commodity.DefaultPrice * NormalPriceCoefficient,
            MidpointRounding.AwayFromZero);

        public MarketCommodity(
            Commodity commodity,
            int targetAmount,
            float maxAmountPercentage,
            float minAmountPercentage,
            int consumption,
            int production,
            float normalPriceCoefficient)
        {
            Commodity = commodity ?? throw new ArgumentNullException(nameof(commodity));
            TargetAmount = targetAmount;
            MaxAmountPercentage = maxAmountPercentage;
            MinAmountPercentage = minAmountPercentage;
            Consumption = consumption;
            Production = production;
            NormalPriceCoefficient = normalPriceCoefficient;
            CurrentAmount = targetAmount;
        }
    }

    public sealed class MarketBehavior : ILocationAction
    {
        public string Title { get; }
        public IReadOnlyList<MarketCommodity> Commodities { get; }

        public MarketBehavior(string title, IReadOnlyList<MarketCommodity> commodities)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Commodities = commodities ?? throw new ArgumentNullException(nameof(commodities));
        }
    }

    public sealed class CargoCommodity
    {
        public Commodity Commodity { get; }
        public int Amount { get; set; }

        public CargoCommodity(Commodity commodity, int amount)
        {
            Commodity = commodity ?? throw new ArgumentNullException(nameof(commodity));
            Amount = amount;
        }
    }

    public sealed class MarketTradeSelection
    {
        private readonly Dictionary<Commodity, int> changes =
            new Dictionary<Commodity, int>();
        private readonly Dictionary<Commodity, int> prices =
            new Dictionary<Commodity, int>();

        public IReadOnlyDictionary<Commodity, int> Changes => changes;
        public bool HasChanges => changes.Count > 0;

        public int GetChange(Commodity commodity) =>
            changes.TryGetValue(commodity, out var amount) ? amount : 0;

        public int SelectBuy(
            Transport transport,
            MarketCommodity marketCommodity,
            int requestedAmount)
        {
            if (requestedAmount <= 0) return 0;
            var commodity = marketCommodity.Commodity;
            var existing = GetChange(commodity);
            prices[commodity] = marketCommodity.CurrentPrice;
            if (existing < 0)
            {
                var cancelledSale = Math.Min(requestedAmount, -existing);
                existing += cancelledSale;
                requestedAmount -= cancelledSale;
                SetChange(commodity, existing);
                if (requestedAmount == 0) return cancelledSale;
            }
            var projectedCargo = transport.CurrentCargoAmount + changes.Values.Sum();
            var marketMinimum = (int)Math.Ceiling(
                marketCommodity.TargetAmount *
                marketCommodity.MinAmountPercentage / 100f);
            var availableStock = marketCommodity.CurrentAmount - existing - marketMinimum;
            var cargoSpace = transport.MaxCargoAmount - projectedCargo;
            var selectedCost = GetSelectedCost();
            var affordable = marketCommodity.CurrentPrice <= 0
                ? requestedAmount
                : Math.Max(0, transport.CurrentGold - selectedCost) /
                  marketCommodity.CurrentPrice;
            var amount = Math.Max(
                0,
                Math.Min(requestedAmount, Math.Min(availableStock, Math.Min(cargoSpace, affordable))));
            SetChange(commodity, existing + amount);
            return amount;
        }

        public int SelectSell(
            Transport transport,
            MarketCommodity marketCommodity,
            int requestedAmount)
        {
            if (requestedAmount <= 0) return 0;
            var commodity = marketCommodity.Commodity;
            var existing = GetChange(commodity);
            prices[commodity] = marketCommodity.CurrentPrice;
            if (existing > 0)
            {
                var cancelledPurchase = Math.Min(requestedAmount, existing);
                existing -= cancelledPurchase;
                requestedAmount -= cancelledPurchase;
                SetChange(commodity, existing);
                if (requestedAmount == 0) return cancelledPurchase;
            }
            var cargoAmount = transport.GetCargoAmount(commodity);
            var selectedSales = Math.Max(0, -existing);
            var marketMaximum = (int)Math.Floor(
                marketCommodity.TargetAmount *
                marketCommodity.MaxAmountPercentage / 100f);
            var marketSpace = marketMaximum - (marketCommodity.CurrentAmount - existing);
            var amount = Math.Max(
                0,
                Math.Min(requestedAmount, Math.Min(cargoAmount - selectedSales, marketSpace)));
            SetChange(commodity, existing - amount);
            return amount;
        }

        public void Commit(Transport transport, MarketBehavior market)
        {
            var entries = changes.ToArray();
            foreach (var entry in entries.Where(entry => entry.Value < 0))
                transport.ChangeCargo(entry.Key, entry.Value);
            foreach (var entry in entries.Where(entry => entry.Value > 0))
                transport.ChangeCargo(entry.Key, entry.Value);
            foreach (var entry in entries)
            {
                var marketCommodity = market.Commodities.Single(
                    candidate => candidate.Commodity == entry.Key);
                marketCommodity.CurrentAmount -= entry.Value;
            }
            transport.ChangeGold(-GetSelectedCost());
            changes.Clear();
            prices.Clear();
        }

        public void Clear()
        {
            changes.Clear();
            prices.Clear();
        }

        private int GetSelectedCost() =>
            changes.Sum(entry => entry.Value * prices[entry.Key]);

        private void SetChange(Commodity commodity, int amount)
        {
            if (amount == 0) changes.Remove(commodity);
            else changes[commodity] = amount;
        }
    }
}
