using TalesOfVoyages.Simulation.Core;

namespace TalesOfVoyages.Simulation.Entities
{
    public interface IEntityAction
    {
        string Label { get; }
        void Execute(GameContext context);
    }

    public interface IProvidesEntityActions : IEntityBehavior
    {
        System.Collections.Generic.IReadOnlyList<IEntityAction> Actions { get; }
    }

    public sealed class EnterPortAction : IEntityAction
    {
        public string Label => "Enter port";

        public void Execute(GameContext context)
        {
            context.Time.SkipToNextDayStart();
        }
    }
}
