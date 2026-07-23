namespace TalesOfVoyages.Simulation.Entities
{
    public interface IEntityBehavior { }

    public sealed class PlayerControlledBehavior : IEntityBehavior { }

    public sealed class TransportBehavior : IEntityBehavior
    {
        public float SpeedPerDay { get; }
        public TransportBehavior(float speedPerDay) => SpeedPerDay = speedPerDay;
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

    public sealed class PortBehavior : IProvidesEntityActions
    {
        private static readonly IEntityAction[] portActions = { new EnterPortAction() };
        public string PortViewSprite { get; }
        public System.Collections.Generic.IReadOnlyList<IEntityAction> Actions => portActions;
        public PortBehavior(string portViewSprite) => PortViewSprite = portViewSprite;
    }
}
