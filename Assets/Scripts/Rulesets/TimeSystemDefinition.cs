using System;
using System.Collections.Generic;
using TalesOfTheBrave.Simulation.Time;
using UnityEngine;

namespace TalesOfTheBrave.Simulation.Rulesets
{
    [Serializable]
    public sealed class TimeSystemDefinition
    {
        public float SecondsPerDay = 7.5f;
        public int DaysPerMonth = 30;
        public int HoursPerDay = 24;
        public int DayStartHourOffset = 7;
        public float MidnightHour = 0f;
        public float NightDarkeningDurationHours = 3f;
        public float NightBrighteningDurationHours = 3f;
        public Color NightTint = new Color(0.0f, 0.0f, 0.2f, 0.3f);
        public List<TimeSpeed> AllowedSpeeds = new List<TimeSpeed>
        {
            TimeSpeed.Normal,
            TimeSpeed.Fast,
            TimeSpeed.VeryFast
        };
    }
}
