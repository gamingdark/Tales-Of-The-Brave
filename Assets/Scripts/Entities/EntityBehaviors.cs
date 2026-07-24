using System;
using System.Collections.Generic;
using System.Linq;
using TalesOfTheBrave.Simulation.Movement;

namespace TalesOfTheBrave.Simulation.Entities
{
    public interface IEntityBehavior { }
    public interface ICargoItem
    {
        string Name { get; }
        string UnitName { get; }
        string UnitAbbreviation { get; }
        string IconSprite { get; }
    }
    public interface ILocationAction : IEntityBehavior
    {
        string Title { get; }
        string IconSprite { get; }
        string AdditionalInfo { get; }
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
        public string Description { get; }
        public System.Collections.Generic.IReadOnlyList<IEntityAction> Actions => locationActions;
        public LocationBehavior(
            string locationViewSprite,
            string description,
            string entityId,
            string displayName)
        {
            LocationViewSprite = locationViewSprite;
            Description = description;
            locationActions = new IEntityAction[]
            {
                new EnterLocationAction(entityId),
                new GoIntoLocationAction(entityId, displayName)
            };
        }
    }

    public sealed class Commodity : ICargoItem
    {
        public string Name { get; }
        public int DefaultPrice { get; }
        public string UnitName { get; }
        public string UnitAbbreviation { get; }
        public string IconSprite { get; }

        public Commodity(
            string name,
            int defaultPrice,
            string unitName,
            string unitAbbreviation,
            string iconSprite = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DefaultPrice = defaultPrice;
            UnitName = unitName ?? throw new ArgumentNullException(nameof(unitName));
            UnitAbbreviation = unitAbbreviation ?? throw new ArgumentNullException(nameof(unitAbbreviation));
            IconSprite = iconSprite;
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
        public float BuySellSpreadPercentage { get; }
        public int CurrentAmount { get; set; }
        public int CurrentPrice { get; internal set; }
        public int BuyPrice => Math.Max(
            0,
            (int)Math.Round(
                CurrentPrice * (1f + BuySellSpreadPercentage / 100f),
                MidpointRounding.AwayFromZero));
        public int SellPrice => Math.Max(
            0,
            (int)Math.Round(
                CurrentPrice * (1f - BuySellSpreadPercentage / 100f),
                MidpointRounding.AwayFromZero));

        public MarketCommodity(
            Commodity commodity,
            int targetAmount,
            float maxAmountPercentage,
            float minAmountPercentage,
            int consumption,
            int production,
            float normalPriceCoefficient,
            float buySellSpreadPercentage = 5f)
        {
            Commodity = commodity ?? throw new ArgumentNullException(nameof(commodity));
            TargetAmount = targetAmount;
            MaxAmountPercentage = maxAmountPercentage;
            MinAmountPercentage = minAmountPercentage;
            Consumption = consumption;
            Production = production;
            NormalPriceCoefficient = normalPriceCoefficient;
            BuySellSpreadPercentage = buySellSpreadPercentage;
            CurrentAmount = targetAmount;
            CurrentPrice = (int)Math.Round(
                Commodity.DefaultPrice * NormalPriceCoefficient,
                MidpointRounding.AwayFromZero);
        }
    }

    public sealed class MarketBehavior : ILocationAction
    {
        public string Title { get; }
        public string IconSprite { get; }
        public string AdditionalInfo => string.Join(
            "\n",
            Commodities.Select(commodity =>
                $"{commodity.Commodity.Name}: {commodity.CurrentAmount}"));
        public IReadOnlyList<MarketCommodity> Commodities { get; }

        public MarketBehavior(
            string title,
            IReadOnlyList<MarketCommodity> commodities,
            string iconSprite = null)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Commodities = commodities ?? throw new ArgumentNullException(nameof(commodities));
            IconSprite = iconSprite;
        }
    }

    public class CargoItemStack
    {
        public ICargoItem Item { get; }
        public int Amount { get; set; }

        public CargoItemStack(ICargoItem item, int amount)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Amount = amount;
        }
    }

    public sealed class CargoCommodity : CargoItemStack
    {
        public Commodity Commodity => (Commodity)Item;

        public CargoCommodity(Commodity commodity, int amount)
            : base(commodity, amount)
        {
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
        public int GoldChange => -GetSelectedCost();

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
            if (existing < 0)
            {
                var cancelledSale = Math.Min(requestedAmount, -existing);
                existing += cancelledSale;
                requestedAmount -= cancelledSale;
                SetChange(commodity, existing, marketCommodity);
                if (requestedAmount == 0) return cancelledSale;
            }
            var projectedCargo = transport.CurrentCargoAmount + changes.Values.Sum();
            var marketMinimum = (int)Math.Ceiling(
                marketCommodity.TargetAmount *
                marketCommodity.MinAmountPercentage / 100f);
            var availableStock = marketCommodity.CurrentAmount - existing - marketMinimum;
            var cargoSpace = transport.MaxCargoAmount - projectedCargo;
            var selectedCost = GetSelectedCost();
            var affordable = marketCommodity.BuyPrice <= 0
                ? requestedAmount
                : Math.Max(0, transport.CurrentGold - selectedCost) /
                  marketCommodity.BuyPrice;
            var amount = Math.Max(
                0,
                Math.Min(requestedAmount, Math.Min(availableStock, Math.Min(cargoSpace, affordable))));
            SetChange(commodity, existing + amount, marketCommodity);
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
            if (existing > 0)
            {
                var cancelledPurchase = Math.Min(requestedAmount, existing);
                existing -= cancelledPurchase;
                requestedAmount -= cancelledPurchase;
                SetChange(commodity, existing, marketCommodity);
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
            SetChange(commodity, existing - amount, marketCommodity);
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

        private void SetChange(
            Commodity commodity,
            int amount,
            MarketCommodity marketCommodity)
        {
            if (amount == 0)
            {
                changes.Remove(commodity);
                prices.Remove(commodity);
            }
            else
            {
                changes[commodity] = amount;
                prices[commodity] = amount > 0
                    ? marketCommodity.BuyPrice
                    : marketCommodity.SellPrice;
            }
        }
    }
}
