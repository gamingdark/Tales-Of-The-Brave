using System;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfVoyages.Simulation.Entities
{
    public sealed class Entity
    {
        private readonly Dictionary<Type, IEntityBehavior> behaviors = new Dictionary<Type, IEntityBehavior>();

        public string Id { get; }
        public string DisplayName { get; }
        public IEnumerable<IEntityBehavior> Behaviors => behaviors.Values;
        public IReadOnlyList<IEntityAction> Actions => behaviors.Values
            .OfType<IProvidesEntityActions>()
            .SelectMany(behavior => behavior.Actions)
            .ToArray();

        public Entity(string id, string displayName)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("An entity ID is required.", nameof(id)) : id;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }

        public void AddBehavior(IEntityBehavior behavior)
        {
            if (behavior == null) throw new ArgumentNullException(nameof(behavior));
            behaviors.Add(behavior.GetType(), behavior);
        }

        public bool HasBehavior<T>() where T : class, IEntityBehavior => behaviors.ContainsKey(typeof(T));

        public T GetBehavior<T>() where T : class, IEntityBehavior
        {
            return behaviors.TryGetValue(typeof(T), out var behavior)
                ? (T)behavior
                : throw new InvalidOperationException($"Entity '{Id}' does not have behavior '{typeof(T).Name}'.");
        }
    }
}
