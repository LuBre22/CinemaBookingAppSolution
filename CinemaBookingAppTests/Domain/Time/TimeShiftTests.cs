using CinemaBookingApp.Domain.Time;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Time;

public class TimeShiftTests
{
    [Fact]
    public void AddMinutes_ShouldAdvanceTimeAcrossDayBoundary()
    {
        var time = new DateTime(2026, 11, 26, 23, 45, 0, DateTimeKind.Utc);

        TimeShift.AddMinutes(ref time, 30);

        time.Should().Be(new DateTime(2026, 11, 27, 0, 15, 0, DateTimeKind.Utc));
        time.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AddMinutes_ShouldLeaveTimeUnchangedForZeroMinutes()
    {
        var time = new DateTime(2026, 11, 26, 12, 0, 0);
        var originalTime = time;

        TimeShift.AddMinutes(ref time, 0);

        time.Should().Be(originalTime);
    }

    [Fact]
    public void AddMinutes_ShouldRejectNegativeMinutesWithoutChangingTime()
    {
        var time = new DateTime(2026, 11, 26, 12, 0, 0);
        var originalTime = time;
        Action act = () => TimeShift.AddMinutes(ref time, -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("minutes");
        time.Should().Be(originalTime);
    }

    [Fact]
    public void AddMinutes_ShouldRejectOverflowWithoutChangingTime()
    {
        var time = DateTime.MaxValue;
        Action act = () => TimeShift.AddMinutes(ref time, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
        time.Should().Be(DateTime.MaxValue);
    }
}
