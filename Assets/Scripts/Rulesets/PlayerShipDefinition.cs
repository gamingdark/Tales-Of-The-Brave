using System;

namespace TalesOfVoyages.Simulation.Rulesets
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
            string mapIconSprite = null)
            : base(id, displayName, new EntityBehaviorsDefinition
            {
                PlayerControlledBehavior = new PlayerControlledBehaviorDefinition(speedPerDay),
                DrawableBehavior = new DrawableBehaviorDefinition(mapIconSprite),
                WorldEntityBehavior = new WorldEntityBehaviorDefinition(startingNodeId)
            })
        {
        }
    }
}
