using System;
using System.Collections.Generic;
using TalesOfVoyages.Simulation.Time;

namespace TalesOfVoyages.Simulation.Rulesets
{
    [Serializable]
    public sealed class TimeSystemDefinition
    {
        public float SecondsPerDay = 7.5f;
        public int DaysPerMonth = 30;
        public int HoursPerDay = 24;
        public int DayStartHourOffset = 7;
        public List<TimeSpeed> AllowedSpeeds = new List<TimeSpeed>
        {
            TimeSpeed.Normal,
            TimeSpeed.Fast,
            TimeSpeed.VeryFast
        };
    }
}
