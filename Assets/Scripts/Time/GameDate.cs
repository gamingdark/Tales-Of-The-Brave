using System;

namespace TalesOfVoyages.Simulation.Time
{
    [Serializable]
    public readonly struct GameDate : IEquatable<GameDate>
    {
        public const int DefaultDaysPerMonth = 30;
        public const int MonthsPerYear = 12;

        public int TotalDays { get; }
        public int DaysPerMonth { get; }
        public int Day => TotalDays % DaysPerMonth + 1;
        public int Month => TotalDays / DaysPerMonth % MonthsPerYear + 1;
        public int Year => TotalDays / (DaysPerMonth * MonthsPerYear) + 1;

        public GameDate(int totalDays, int daysPerMonth = DefaultDaysPerMonth)
        {
            if (totalDays < 0) throw new ArgumentOutOfRangeException(nameof(totalDays));
            if (daysPerMonth <= 0) throw new ArgumentOutOfRangeException(nameof(daysPerMonth));
            TotalDays = totalDays;
            DaysPerMonth = daysPerMonth;
        }

        public GameDate AddDays(int days) => new GameDate(TotalDays + days, DaysPerMonth);
        public bool Equals(GameDate other) => TotalDays == other.TotalDays && DaysPerMonth == other.DaysPerMonth;
        public override bool Equals(object obj) => obj is GameDate other && Equals(other);
        public override int GetHashCode() => (TotalDays * 397) ^ DaysPerMonth;
        public override string ToString() => $"Day {Day}, Month {Month}, Year {Year}";
    }
}
