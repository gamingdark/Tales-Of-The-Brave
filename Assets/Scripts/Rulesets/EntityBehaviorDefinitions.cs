using System;

namespace TalesOfVoyages.Simulation.Rulesets
{
    [Serializable]
    public sealed class EntityBehaviorsDefinition
    {
        public PlayerControlledBehaviorDefinition PlayerControlledBehavior;
        public TransportBehaviorDefinition TransportBehavior;
        public DrawableBehaviorDefinition DrawableBehavior;
        public WorldEntityBehaviorDefinition WorldEntityBehavior;
        public PortBehaviorDefinition PortBehavior;
    }

    [Serializable]
    public sealed class PlayerControlledBehaviorDefinition { }

    [Serializable]
    public sealed class TransportBehaviorDefinition
    {
        public float SpeedPerDay;
        public TransportBehaviorDefinition() { }
        public TransportBehaviorDefinition(float speedPerDay) => SpeedPerDay = speedPerDay;
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
    public sealed class PortBehaviorDefinition
    {
        public string PortViewSprite;
        public PortBehaviorDefinition() { }
        public PortBehaviorDefinition(string portViewSprite) => PortViewSprite = portViewSprite;
    }
}
