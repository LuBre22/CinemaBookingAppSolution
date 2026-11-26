using CinemaBookingApp.Domain.Time;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Time;

public class StartingTimeTests
{
    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(31, true)]
    public void IsValid_ShouldEnforceThirtyMinuteBuffer(int minutesAfterEnd, bool expected)
    {
        var end = DateTime.Now.AddDays(2);

        StartingTime.IsValid(end, end.AddMinutes(minutesAfterEnd)).Should().Be(expected);
    }

    [Fact]
    public void IsValid_ShouldRejectPastStartEvenWhenBufferIsSatisfied()
    {
        var start = DateTime.Now.AddDays(-1);

        StartingTime.IsValid(start.AddHours(-1), start).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldHandleBufferAcrossMidnight()
    {
        var end = DateTime.Today.AddDays(2).AddHours(23).AddMinutes(45);

        StartingTime.IsValid(end, end.AddMinutes(30)).Should().BeTrue();
        StartingTime.IsValid(end, end.AddMinutes(29)).Should().BeFalse();
    }
}
