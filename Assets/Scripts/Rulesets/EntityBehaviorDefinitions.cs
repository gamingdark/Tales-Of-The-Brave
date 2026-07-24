using System;
using System.Collections.Generic;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class EntityBehaviorsDefinition
    {
        public PlayerControlledBehaviorDefinition PlayerControlledBehavior;
        public TransportBehaviorDefinition TransportBehavior;
        public DrawableBehaviorDefinition DrawableBehavior;
        public WorldEntityBehaviorDefinition WorldEntityBehavior;
        public LocationBehaviorDefinition LocationBehavior;
        public MarketBehaviorDefinition MarketBehavior;
    }

    [Serializable]
    public sealed class PlayerControlledBehaviorDefinition { }

    [Serializable]
    public sealed class TransportBehaviorDefinition
    {
        public float SpeedPerDay;
        public int MaxCargoAmount = 200;
        public int CurrentGold = 1000;
        public TransportBehaviorDefinition() { }
        public TransportBehaviorDefinition(
            float speedPerDay,
            int maxCargoAmount = 200,
            int currentGold = 1000)
        {
            SpeedPerDay = speedPerDay;
            MaxCargoAmount = maxCargoAmount;
            CurrentGold = currentGold;
        }
    }

    [Serializable]
    public sealed class DrawableBehaviorDefinition
    {
        public string MapIconSprite;
        public DrawableBehaviorDefinition() { }
        public DrawableBehaviorDefinition(string mapIconSprite) => MapIconSprite = mapIconSprite;
    }

    [Serializable]
    public sealed class WorldEntityBehaviorDefinition
    {
        public string StartingNodeId;
        public WorldEntityBehaviorDefinition() { }
        public WorldEntityBehaviorDefinition(string startingNodeId) => StartingNodeId = startingNodeId;
    }

    [Serializable]
    public sealed class LocationBehaviorDefinition
    {
        public string LocationViewSprite;
        public string Description;
        public LocationBehaviorDefinition() { }
        public LocationBehaviorDefinition(
            string locationViewSprite,
            string description = null)
        {
            LocationViewSprite = locationViewSprite;
            Description = description;
        }
    }

    [Serializable]
    public sealed class MarketBehaviorDefinition
    {
        public string Title = "Market";
        public string IconSprite;
        public List<MarketCommodityDefinition> Commodities = new List<MarketCommodityDefinition>();

        public MarketBehaviorDefinition() { }

        public MarketBehaviorDefinition(
            IEnumerable<string> commodityNames,
            string title = "Market",
            string iconSprite = null)
        {
            Title = title;
            IconSprite = iconSprite;
            if (commodityNames == null) return;
            foreach (var commodityName in commodityNames)
                Commodities.Add(new MarketCommodityDefinition(commodityName));
        }
    }

    [Serializable]
    public sealed class MarketCommodityDefinition
    {
        public string CommodityName;
        public int TargetAmount = 100;
        public float MaxAmountPercentage = 200f;
        public float MinAmountPercentage = 50f;
        public int Consumption = 25;
        public int Production = 25;
        public float NormalPriceCoefficient = 1f;

        public MarketCommodityDefinition() { }
        public MarketCommodityDefinition(string commodityName) => CommodityName = commodityName;
    }
}
