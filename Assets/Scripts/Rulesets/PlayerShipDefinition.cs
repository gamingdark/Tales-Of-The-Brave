using System;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class PlayerShipDefinition : EntityDefinition
    {
        public PlayerShipDefinition() { }

        public PlayerShipDefinition(
            string id,
            string displayName,
            float speedPerDay,
            string startingNodeId,
            string mapIconSprite = null,
            int maxCargoAmount = 200,
            int currentGold = 1000)
            : base(id, displayName, new EntityBehaviorsDefinition
            {
                PlayerControlledBehavior = new PlayerControlledBehaviorDefinition(),
                TransportBehavior = new TransportBehaviorDefinition(
                    speedPerDay, maxCargoAmount, currentGold),
                DrawableBehavior = new DrawableBehaviorDefinition(mapIconSprite),
                WorldEntityBehavior = new WorldEntityBehaviorDefinition(startingNodeId)
            })
        {
        }
    }
}
