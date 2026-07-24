using NUnit.Framework;
using TalesOfTheBrave.Simulation.Time;

public sealed class TimeManagerTests
{
    [Test]
    public void Tick_EmitsExactlyOnePulsePerElapsedDay()
    {
        var time = new TimeManager(30f);
        var pulses = 0;
        time.DayAdvanced += _ => pulses++;
        time.Tick(75f);
        Assert.That(time.CurrentDate.TotalDays, Is.EqualTo(2));
        Assert.That(pulses, Is.EqualTo(2));
    }

    [Test]
    public void PausedTimeDoesNotAdvance()
    {
        var time = new TimeManager();
        time.SetSpeed(TimeSpeed.Paused);
        time.Tick(300f);
        Assert.That(time.CurrentDate.TotalDays, Is.Zero);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("07:00"));
    }

    [Test]
    public void FormattedTimeShowsHourlyProgressWithinTheDay()
    {
        var time = new TimeManager(24f, dayStartHourOffset: 0);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("00:00"));

        time.Tick(7f);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("07:00"));

        time.SetSpeed(TimeSpeed.Fast);
        time.Tick(2.5f);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("12:00"));
    }

    [Test]
    public void FormattedTimeReturnsToConfiguredStartHourWhenDayAdvances()
    {
        var time = new TimeManager(24f);
        time.Tick(24f);

        Assert.That(time.CurrentDate.TotalDays, Is.EqualTo(1));
        Assert.That(time.GetFormattedTime(), Is.EqualTo("07:00"));
    }

    [Test]
    public void DisplayClockUsesConfiguredHoursAndDayStartOffsetWithoutChangingDayProgress()
    {
        var time = new TimeManager(10f, hoursPerDay: 10, dayStartHourOffset: 3);

        time.Tick(5f);

        Assert.That(time.DayProgress, Is.EqualTo(0.5f));
        Assert.That(time.GetFormattedTime(), Is.EqualTo("08:00"));
        Assert.That(time.CurrentDate.TotalDays, Is.Zero);
    }

    [Test]
    public void ConfiguredDaysPerMonthControlsDisplayedDate()
    {
        var time = new TimeManager(10f, daysPerMonth: 20);

        time.AdvanceDays(20);

        Assert.That(time.CurrentDate.Day, Is.EqualTo(1));
        Assert.That(time.CurrentDate.Month, Is.EqualTo(2));
    }

    [Test]
    public void PauseIsAlwaysAllowedButUnconfiguredRunningSpeedIsRejected()
    {
        var time = new TimeManager(allowedSpeeds: new[] { TimeSpeed.Normal });

        Assert.DoesNotThrow(() => time.SetSpeed(TimeSpeed.Paused));
        Assert.Throws<System.InvalidOperationException>(() => time.SetSpeed(TimeSpeed.Fast));
    }

    [Test]
    public void TickCanBeClampedAtSimulationDayEndWithoutAdvancingDate()
    {
        var time = new TimeManager(24f, dayStartHourOffset: 7);

        time.TickUntilDayProgress(24f, 1f);

        Assert.That(time.CurrentDate.TotalDays, Is.Zero);
        Assert.That(time.DayProgress, Is.EqualTo(1f));
        Assert.That(time.GetFormattedTime(), Is.EqualTo("06:00"));
    }
}
