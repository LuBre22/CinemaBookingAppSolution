using CinemaBookingApp.Domain.Time;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Time;

public class StartingTimeTests
{
    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(31, true)]
    public void IsValidBuffer_ShouldEnforceThirtyMinuteBuffer(int minutesAfterEnd, bool expected)
    {
        var end = DateTime.Now.AddDays(2);

        StartingTime.IsValidBuffer(end, end.AddMinutes(minutesAfterEnd)).Should().Be(expected);
    }

    [Fact]
    public void IsFutureTime_ShouldRejectPastStart()
    {
        var start = DateTime.Now.AddDays(-1);

        StartingTime.isFutureTime(start).Should().BeFalse();
    }

    [Fact]
    public void IsValidBuffer_ShouldHandleBufferAcrossMidnight()
    {
        var end = DateTime.Today.AddDays(2).AddHours(23).AddMinutes(45);

        StartingTime.IsValidBuffer(end, end.AddMinutes(30)).Should().BeTrue();
        StartingTime.IsValidBuffer(end, end.AddMinutes(29)).Should().BeFalse();
    }

    [Fact]
    public void IsFutureTime_ShouldAcceptFutureStart()
    {
        StartingTime.isFutureTime(DateTime.Now.AddDays(1)).Should().BeTrue();
    }
}
