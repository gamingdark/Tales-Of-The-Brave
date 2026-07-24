using System;
using System.Collections.Generic;
using System.Linq;

namespace TalesOfTheBrave.Simulation.Time
{
    public sealed class TimeManager
    {
        private float elapsedDayFraction;

        public GameDate CurrentDate { get; private set; }
        public TimeSpeed Speed { get; private set; }
        public float SecondsPerDay { get; }
        public int HoursPerDay { get; }
        public int DayStartHourOffset { get; }
        public IReadOnlyList<TimeSpeed> AllowedSpeeds { get; }
        public float DayProgress => (float)elapsedDayFraction;
        public float CurrentDisplayedHour =>
            (DayStartHourOffset + Math.Min(
                HoursPerDay - 0.0001f,
                DayProgress * HoursPerDay)) % HoursPerDay;
        public int CurrentHour => Math.Min(
            HoursPerDay - 1,
            (int)Math.Floor(CurrentDisplayedHour + 0.0001f));
        public event Action<GameDate> DayAdvanced;

        public TimeManager(
            float secondsPerDay = 15.0f,
            GameDate? startDate = null,
            int daysPerMonth = GameDate.DefaultDaysPerMonth,
            int hoursPerDay = 24,
            int dayStartHourOffset = 7,
            IEnumerable<TimeSpeed> allowedSpeeds = null)
        {
            if (secondsPerDay <= 0.0f) throw new ArgumentOutOfRangeException(nameof(secondsPerDay));
            if (daysPerMonth <= 0) throw new ArgumentOutOfRangeException(nameof(daysPerMonth));
            if (hoursPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(hoursPerDay));
            if (dayStartHourOffset < 0 || dayStartHourOffset >= hoursPerDay)
                throw new ArgumentOutOfRangeException(nameof(dayStartHourOffset));
            SecondsPerDay = secondsPerDay;
            HoursPerDay = hoursPerDay;
            DayStartHourOffset = dayStartHourOffset;
            AllowedSpeeds = (allowedSpeeds ?? new[] { TimeSpeed.Normal, TimeSpeed.Fast, TimeSpeed.VeryFast }).ToArray();
            if (AllowedSpeeds.Count == 0) throw new ArgumentException("At least one running speed is required.", nameof(allowedSpeeds));
            CurrentDate = startDate ?? new GameDate(0, daysPerMonth);
            if (CurrentDate.DaysPerMonth != daysPerMonth)
                throw new ArgumentException("The starting date must use the configured days per month.", nameof(startDate));
            Speed = AllowedSpeeds[0];
        }

        public void SetSpeed(TimeSpeed speed)
        {
            if (speed != TimeSpeed.Paused && !AllowedSpeeds.Contains(speed))
                throw new InvalidOperationException($"Time speed '{speed}' is not allowed by this ruleset.");
            Speed = speed;
        }

        public string GetFormattedTime() => $"{CurrentHour:00}:00";

        public void Tick(float realSeconds)
        {
            TickInternal(realSeconds, null);
        }

        public void TickUntilDayProgress(float realSeconds, float maximumDayProgress)
        {
            if (maximumDayProgress < 0f || maximumDayProgress > 1f)
                throw new ArgumentOutOfRangeException(nameof(maximumDayProgress));
            TickInternal(realSeconds, maximumDayProgress);
        }

        private void TickInternal(float realSeconds, float? maximumDayProgress)
        {
            if (realSeconds < 0.0f) throw new ArgumentOutOfRangeException(nameof(realSeconds));
            if (Speed == TimeSpeed.Paused) return;

            elapsedDayFraction += realSeconds * (int) Speed / SecondsPerDay;
            if (maximumDayProgress.HasValue && elapsedDayFraction >= maximumDayProgress.Value)
            {
                elapsedDayFraction = maximumDayProgress.Value;
                return;
            }
            var wholeDays = (int) elapsedDayFraction;
            if (wholeDays == 0) return;
            elapsedDayFraction -= wholeDays;
            EmitDayPulses(wholeDays);
        }

        public void AdvanceDay() => AdvanceDays(1);

        public void SkipToNextDayStart() => AdvanceDay();

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
