using System;
using System.Collections.Generic;
using TalesOfTheBrave.Simulation.Core;
using TalesOfTheBrave.Simulation.World;
using UnityEngine;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class WorldDefinition
    {
        public List<WorldNodeDefinition> Nodes = new List<WorldNodeDefinition>();
        public List<WorldEdgeDefinition> Edges = new List<WorldEdgeDefinition>();
        public List<EntityDefinition> Entities = new List<EntityDefinition>();
        public List<CommodityDefinition> Commodities = new List<CommodityDefinition>();
        public TimeSystemDefinition TimeSystem = new TimeSystemDefinition();
        public UiSystemDefinition UI = UiSystemDefinition.CreateDefault();
        public string MapBackgroundSprite = "map";
        public string SceneBackgroundSprite = "wooden-background";
        public float MapWidth = 11.2f;
        public float MapHeight = 7.9f;

        public static WorldDefinition CreateDefault()
        {
            return new WorldDefinition
            {
                Commodities = new List<CommodityDefinition>
                {
                    new CommodityDefinition("Timber", 10),
                    new CommodityDefinition("Grain", 8),
                    new CommodityDefinition("Tar", 20, new CommodityUnitDefinition("liters", "l")),
                    new CommodityDefinition("Iron", 25)
                },
                Nodes = new List<WorldNodeDefinition>
                {
                    new WorldNodeDefinition("node_klaipeda", "Klaipėda", WorldNodeType.Location, 0.62f, 0.2f),
                    new WorldNodeDefinition("node_riga", "Riga", WorldNodeType.Location, 0.73f, 0.33f),
                    new WorldNodeDefinition("node_helsinki", "Helsinki", WorldNodeType.Location, 0.70f, 0.63f),
                    new WorldNodeDefinition("node_stockholm", "Stockholm", WorldNodeType.Location, 0.5f, 0.57f),
                    new WorldNodeDefinition("node_irbe_strait", "Irbe Strait", WorldNodeType.Sea, 0.63f, 0.39f),
                    new WorldNodeDefinition("node_west_courland", "West of Courland", WorldNodeType.Sea, 0.59f, 0.32f),
                    new WorldNodeDefinition("node_west_saaremaa", "West of Saaremaa", WorldNodeType.Sea, 0.61f, 0.47f),
                    new WorldNodeDefinition("node_stockholm_archipelago", "Stockholm Archipelago", WorldNodeType.Sea, 0.535f, 0.535f)
                },
                Edges = new List<WorldEdgeDefinition>
                {
                    new WorldEdgeDefinition("route_klaipeda_courland", "node_klaipeda", "node_west_courland", 25f,
                        new[]
                        {
                            new RouteMapPointDefinition(0.59f, 0.21f),
                        }),

                    new WorldEdgeDefinition("route_courland_irbe", "node_west_courland", "node_irbe_strait", 25f),

                    new WorldEdgeDefinition("route_irbe_riga", "node_irbe_strait", "node_riga", 25f,
                        new[]
                        {
                            new RouteMapPointDefinition(0.68f, 0.42f),
                            new RouteMapPointDefinition(0.73f, 0.34f)
                        }),
                    new WorldEdgeDefinition("route_saaremaa_helsinki", "node_west_saaremaa", "node_helsinki", 40f,
                        new[]
                        {
                            new RouteMapPointDefinition(0.64f, 0.56f),
                            new RouteMapPointDefinition(0.69f, 0.59f)
                        }),

                    new WorldEdgeDefinition("route_courland_saaremaa", "node_west_courland", "node_west_saaremaa", 40f),
                    new WorldEdgeDefinition("route_irbe_saaremaa", "node_irbe_strait", "node_west_saaremaa", 25f),
                    new WorldEdgeDefinition("route_stockholm_archipelago", "node_stockholm", "node_stockholm_archipelago", 15f),
                    new WorldEdgeDefinition("route_courland_archipelago", "node_west_courland", "node_stockholm_archipelago", 60f),
                    new WorldEdgeDefinition("route_saaremaa_archipelago", "node_west_saaremaa", "node_stockholm_archipelago", 40f),
                },
                Entities = new List<EntityDefinition>
                {
                    CreateLocation("location_klaipeda", "Klaipėda", "node_klaipeda", "icons.4", "img-klaipeda",
                        "Grain", "Timber"),
                    CreateLocation("location_riga", "Riga", "node_riga", "icons.3", "img-riga",
                        "Tar", "Grain", "Timber"),
                    CreateLocation("location_helsinki", "Helsinki", "node_helsinki", "icons.5", "img-helsinki",
                        "Tar", "Timber", "Iron"),
                    CreateLocation("location_stockholm", "Stockholm", "node_stockholm", "icons.7", "img-stockholm",
                        "Iron", "Timber", "Grain"),
                    new PlayerShipDefinition(
                        GameContext.PlayerShipId, "The Unsinkable", 25f, "node_klaipeda", "icons.8")
                }
            };
        }

        private static EntityDefinition CreateLocation(
            string id,
            string displayName,
            string startingNodeId,
            string mapIconSprite,
            string locationViewSprite,
            params string[] marketCommodities)
        {
            return new EntityDefinition(id, displayName, new EntityBehaviorsDefinition
            {
                LocationBehavior = new LocationBehaviorDefinition(locationViewSprite),
                MarketBehavior = new MarketBehaviorDefinition(marketCommodities),
                DrawableBehavior = new DrawableBehaviorDefinition(mapIconSprite),
                WorldEntityBehavior = new WorldEntityBehaviorDefinition(startingNodeId)
            });
        }
    }

    [Serializable]
    public sealed class CommodityUnitDefinition
    {
        public string FullName = "tonnes";
        public string Abbreviation = "t";

        public CommodityUnitDefinition() { }
        public CommodityUnitDefinition(string fullName, string abbreviation)
        {
            FullName = fullName;
            Abbreviation = abbreviation;
        }
    }

    [Serializable]
    public sealed class CommodityDefinition
    {
        public string Name;
        public int DefaultPrice;
        public CommodityUnitDefinition Unit = new CommodityUnitDefinition();

        public CommodityDefinition() { }
        public CommodityDefinition(
            string name,
            int defaultPrice,
            CommodityUnitDefinition unit = null)
        {
            Name = name;
            DefaultPrice = defaultPrice;
            Unit = unit ?? new CommodityUnitDefinition();
        }
    }

    [Serializable]
    public sealed class UiSystemDefinition
    {
        public UiStyleDefinition Menus = new();
        public UiStyleDefinition Tooltips = new();

        public static UiSystemDefinition CreateDefault()
        {
            return new UiSystemDefinition
            {
                Menus = new UiStyleDefinition
                {
                    Background = new Color(0.055f, 0.07f, 0.1f, 0.88f),
                    Border = new Color(0.015f, 0.015f, 0.015f),
                    BorderWidth = 2f,
                    Font = new Color(0.86f, 0.86f, 0.86f)
                },
                Tooltips = new UiStyleDefinition
                {
                    Background = new Color(0.12f, 0.14f, 0.18f, 0.96f),
                    Border = new Color(0.08f, 0.08f, 0.08f),
                    BorderWidth = 2f,
                    Font = new Color(0.94f, 0.94f, 0.94f)
                }
            };
        }
    }

    [Serializable]
    public sealed class UiStyleDefinition
    {
        public Color Background = new Color();
        public Color Border = new Color();
        public float BorderWidth = 2f;
        public Color Font = new Color(1f, 1f, 1f);
    }
}
