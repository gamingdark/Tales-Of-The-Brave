using TalesOfTheBrave.Simulation.Core;

namespace TalesOfTheBrave.Simulation.Entities
{
    public interface IEntityAction
    {
        string Label { get; }
        bool IsAvailable(GameContext context);
        void Execute(GameContext context);
    }

    public interface IProvidesEntityActions : IEntityBehavior
    {
        System.Collections.Generic.IReadOnlyList<IEntityAction> Actions { get; }
    }

    public sealed class EnterLocationAction : IEntityAction
    {
        private readonly string locationEntityId;
        public string Label => "Enter location";

        public EnterLocationAction(string locationEntityId) =>
            this.locationEntityId = locationEntityId;

        public bool IsAvailable(GameContext context) =>
            context.GetPendingInteractionEntity()?.Id == locationEntityId;

        public void Execute(GameContext context)
        {
            if (!IsAvailable(context))
                throw new System.InvalidOperationException("This location is not awaiting arrival.");
            context.Time.SkipToNextDayStart();
            context.EnterLocation(locationEntityId);
        }
    }

    public sealed class GoIntoLocationAction : IEntityAction
    {
        private readonly string locationEntityId;
        private readonly string locationName;
        public string Label => $"Go into {locationName}";

        public GoIntoLocationAction(string locationEntityId, string locationName)
        {
            this.locationEntityId = locationEntityId;
            this.locationName = locationName;
        }

        public bool IsAvailable(GameContext context) =>
            !context.PlayerShip.Travel.IsTravelling &&
            !context.PlayerShip.Travel.IsInsideLocation &&
            context.GetLocationAtNode(context.PlayerShip.Travel.CurrentNodeId)?.Id == locationEntityId;

        public void Execute(GameContext context)
        {
            if (!IsAvailable(context))
                throw new System.InvalidOperationException("The ship is not at this location.");
            context.EnterLocation(locationEntityId);
        }
    }
}
