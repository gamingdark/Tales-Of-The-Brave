using System;
using System.Collections.Generic;
using TalesOfVoyages.Simulation.Core;
using TalesOfVoyages.Simulation.World;

namespace TalesOfVoyages.Simulation.Rulesets
{
    [Serializable]
    public sealed class MvpWorldDefinition
    {
        public List<WorldNodeDefinition> Nodes = new List<WorldNodeDefinition>();
        public List<WorldEdgeDefinition> Edges = new List<WorldEdgeDefinition>();
        public List<EntityDefinition> Entities = new List<EntityDefinition>();
        public TimeSystemDefinition TimeSystem = new TimeSystemDefinition();
        public string MapBackgroundSprite = "map";
        public string SceneBackgroundSprite = "wooden-background";

        public static MvpWorldDefinition CreateDefault()
        {
            return new MvpWorldDefinition
            {
                Nodes = new List<WorldNodeDefinition>
                {
                    new WorldNodeDefinition("node_klaipeda", "Klaipėda", WorldNodeType.Port, 0.62f, 0.2f),
                    new WorldNodeDefinition("node_riga", "Riga", WorldNodeType.Port, 0.73f, 0.33f),
                    new WorldNodeDefinition("node_helsinki", "Helsinki", WorldNodeType.Port, 0.70f, 0.63f),
                    new WorldNodeDefinition("node_irbe_strait", "Irbe Strait", WorldNodeType.Sea, 0.63f, 0.39f),
                    new WorldNodeDefinition("node_west_courland", "West of Courland", WorldNodeType.Sea, 0.59f, 0.32f),
                    new WorldNodeDefinition("node_west_saaremaa", "West of Saaremaa", WorldNodeType.Sea, 0.61f, 0.47f)
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
                },
                Entities = new List<EntityDefinition>
                {
                    CreatePort("port_klaipeda", "Klaipėda", "node_klaipeda", "icons.4", "img-klaipeda"),
                    CreatePort("port_riga", "Riga", "node_riga", "icons.3", "img-riga"),
                    CreatePort("port_helsinki", "Helsinki", "node_helsinki", "icons.5", "img-helsinki"),
                    new PlayerShipDefinition(
                        GameContext.PlayerShipId, "The Unsinkable MVP", 25f, "node_klaipeda", "icons.8")
                }
            };
        }

        private static EntityDefinition CreatePort(
            string id, string displayName, string startingNodeId, string mapIconSprite, string portViewSprite)
        {
            return new EntityDefinition(id, displayName, new EntityBehaviorsDefinition
            {
                PortBehavior = new PortBehaviorDefinition(portViewSprite),
                DrawableBehavior = new DrawableBehaviorDefinition(mapIconSprite),
                WorldEntityBehavior = new WorldEntityBehaviorDefinition(startingNodeId)
            });
        }
    }
}
