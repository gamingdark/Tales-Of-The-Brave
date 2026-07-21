using NUnit.Framework;
using TalesOfVoyages.Simulation.Time;

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
        Assert.That(time.GetFormattedTime(), Is.EqualTo("00:00"));
    }

    [Test]
    public void FormattedTimeShowsHourlyProgressWithinTheDay()
    {
        var time = new TimeManager(24f);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("00:00"));

        time.Tick(7f);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("07:00"));

        time.SetSpeed(TimeSpeed.Fast);
        time.Tick(2.5f);
        Assert.That(time.GetFormattedTime(), Is.EqualTo("12:00"));
    }

    [Test]
    public void FormattedTimeReturnsToMidnightWhenDayAdvances()
    {
        var time = new TimeManager(24f);
        time.Tick(24f);

        Assert.That(time.CurrentDate.TotalDays, Is.EqualTo(1));
        Assert.That(time.GetFormattedTime(), Is.EqualTo("00:00"));
    }
}
