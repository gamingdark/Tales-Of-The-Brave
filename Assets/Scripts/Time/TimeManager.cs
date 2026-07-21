using System;

namespace TalesOfVoyages.Simulation.Time
{
    public sealed class TimeManager
    {
        private float elapsedDayFraction;

        public GameDate CurrentDate { get; private set; }
        public TimeSpeed Speed { get; private set; }
        public float SecondsPerDay { get; }
        public float DayProgress => (float)elapsedDayFraction;
        public int CurrentHour => Math.Min(23, (int)(elapsedDayFraction * 24.0f));
        public event Action<GameDate> DayAdvanced;

        public TimeManager(float secondsPerDay = 15.0f, GameDate? startDate = null)
        {
            if (secondsPerDay <= 0.0f) throw new ArgumentOutOfRangeException(nameof(secondsPerDay));
            SecondsPerDay = secondsPerDay;
            CurrentDate = startDate ?? new GameDate(0);
            Speed = TimeSpeed.Normal;
        }

        public void SetSpeed(TimeSpeed speed) => Speed = speed;

        public string GetFormattedTime() => $"{CurrentHour:00}:00";

        public void Tick(float realSeconds)
        {
            if (realSeconds < 0.0f) throw new ArgumentOutOfRangeException(nameof(realSeconds));
            if (Speed == TimeSpeed.Paused) return;

            elapsedDayFraction += realSeconds * (int) Speed / SecondsPerDay;
            var wholeDays = (int) elapsedDayFraction;
            if (wholeDays == 0) return;
            elapsedDayFraction -= wholeDays;
            EmitDayPulses(wholeDays);
        }

        public void AdvanceDay() => AdvanceDays(1);

        public void AdvanceDays(int days)
        {
            if (days < 0) throw new ArgumentOutOfRangeException(nameof(days));
            elapsedDayFraction = 0.0f;
            EmitDayPulses(days);
        }

        private void EmitDayPulses(int days)
        {
            for (var i = 0; i < days; i++)
            {
                CurrentDate = CurrentDate.AddDays(1);
                DayAdvanced?.Invoke(CurrentDate);
            }
        }
    }
}
