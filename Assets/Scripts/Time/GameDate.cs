using System;

namespace TalesOfVoyages.Simulation.Time
{
    [Serializable]
    public readonly struct GameDate : IEquatable<GameDate>
    {
        public const int DaysPerMonth = 30;
        public const int MonthsPerYear = 12;

        public int TotalDays { get; }
        public int Day => TotalDays % DaysPerMonth + 1;
        public int Month => TotalDays / DaysPerMonth % MonthsPerYear + 1;
        public int Year => TotalDays / (DaysPerMonth * MonthsPerYear) + 1;

        public GameDate(int totalDays)
        {
            if (totalDays < 0) throw new ArgumentOutOfRangeException(nameof(totalDays));
            TotalDays = totalDays;
        }

        public GameDate AddDays(int days) => new GameDate(TotalDays + days);
        public bool Equals(GameDate other) => TotalDays == other.TotalDays;
        public override bool Equals(object obj) => obj is GameDate other && Equals(other);
        public override int GetHashCode() => TotalDays;
        public override string ToString() => $"Day {Day}, Month {Month}, Year {Year}";
    }
}
