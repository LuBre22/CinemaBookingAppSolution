using CinemaBookingApp.Domain.Time;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Time;

public class TimeShiftTests
{
    [Fact]
    public void Add_ShouldAdvanceTimeAcrossDayBoundary()
    {
        var time = new DateTime(2026, 11, 26, 23, 45, 0, DateTimeKind.Utc);

        TimeShift.Add(ref time, TimeSpan.FromMinutes(30));

        time.Should().Be(new DateTime(2026, 11, 27, 0, 15, 0, DateTimeKind.Utc));
        time.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Add_ShouldLeaveTimeUnchangedForZeroDuration()
    {
        var time = new DateTime(2026, 11, 26, 12, 0, 0);
        var originalTime = time;

        TimeShift.Add(ref time, TimeSpan.Zero);

        time.Should().Be(originalTime);
    }

    [Fact]
    public void Add_ShouldMoveTimeBackwardForNegativeDuration()
    {
        var time = new DateTime(2026, 11, 26, 12, 0, 0);

        TimeShift.Add(ref time, TimeSpan.FromMinutes(-1));

        time.Should().Be(new DateTime(2026, 11, 26, 11, 59, 0));
    }

    [Fact]
    public void Add_ShouldPreserveSubMinutePrecision()
    {
        var time = new DateTime(2026, 11, 26, 12, 0, 0);

        TimeShift.Add(ref time, TimeSpan.FromSeconds(30));

        time.Should().Be(new DateTime(2026, 11, 26, 12, 0, 30));
    }

    [Fact]
    public void Add_ShouldRejectOverflowWithoutChangingTime()
    {
        var time = DateTime.MaxValue;
        Action act = () => TimeShift.Add(ref time, TimeSpan.FromMinutes(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
        time.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public void Add_ShouldRejectUnderflowWithoutChangingTime()
    {
        var time = DateTime.MinValue;
        Action act = () => TimeShift.Add(ref time, TimeSpan.FromTicks(-1));

        act.Should().Throw<ArgumentOutOfRangeException>();
        time.Should().Be(DateTime.MinValue);
    }
}
