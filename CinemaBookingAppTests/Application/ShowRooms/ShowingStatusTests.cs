using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.ShowRooms;

public class ShowingStatusTests
{
    [Fact]
    public void Status_ShouldFollowStartAndEndBoundaries()
    {
        var start = new DateTime(2030, 1, 1, 12, 0, 0);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, start, 10m);

        showing.GetStatus(start.AddTicks(-1)).Should().Be(FilmStatusCategories.PLANNED);
        showing.GetStatus(start).Should().Be(FilmStatusCategories.RUNNING);
        showing.GetStatus(showing.EndingTime.AddTicks(-1)).Should().Be(FilmStatusCategories.RUNNING);
        showing.GetStatus(showing.EndingTime).Should().Be(FilmStatusCategories.OVER);
    }

    [Fact]
    public void CancelledStatus_ShouldRemainCancelledAfterEndTime()
    {
        var start = DateTime.Now.AddDays(1);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, start, 10m);

        showing.Cancel();

        showing.Status.Should().Be(FilmStatusCategories.CANCELLED);
        showing.GetStatus(start.AddDays(1)).Should().Be(FilmStatusCategories.CANCELLED);
        Action cancelAgain = () => showing.Cancel();
        cancelAgain.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(-90)]
    public void Cancel_ShouldRejectRunningAndOverShowings(int startOffsetMinutes)
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Now.AddMinutes(startOffsetMinutes), 10m);

        Action act = () => showing.Cancel();

        act.Should().Throw<InvalidOperationException>();
        showing.Status.Should().NotBe(FilmStatusCategories.CANCELLED);
    }
}
