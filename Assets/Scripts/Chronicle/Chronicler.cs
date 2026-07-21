using System.Collections.Generic;
using TalesOfVoyages.Simulation.Time;

namespace TalesOfVoyages.Simulation.Chronicle
{
    public sealed class Chronicler
    {
        private readonly List<ChronicleEntry> entries = new List<ChronicleEntry>();
        public IReadOnlyList<ChronicleEntry> Entries => entries;
        public void Record(GameDate date, string eventType, string text) => entries.Add(new ChronicleEntry(date, eventType, text));
    }
}
