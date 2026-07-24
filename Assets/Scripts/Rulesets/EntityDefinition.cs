using System;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public class EntityDefinition
    {
        public string Id;
        public string DisplayName;
        public EntityBehaviorsDefinition Behaviors = new EntityBehaviorsDefinition();

        public EntityDefinition() { }

        public EntityDefinition(string id, string displayName, EntityBehaviorsDefinition behaviors)
        {
            Id = id;
            DisplayName = displayName;
            Behaviors = behaviors;
        }
    }
}
