using TalesOfVoyages.Simulation.Time;

namespace TalesOfVoyages.Simulation.Chronicle
{
    public readonly struct ChronicleEntry
    {
        public GameDate Date { get; }
        public string EventType { get; }
        public string Text { get; }
        public ChronicleEntry(GameDate date, string eventType, string text) { Date = date; EventType = eventType; Text = text; }
    }
}
