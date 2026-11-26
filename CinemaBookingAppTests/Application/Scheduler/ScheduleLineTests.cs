using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleLineTests
{
    [Fact]
    public void Constructor_ShouldPreserveRoomAndDate()
    {
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var date = new DateOnly(2030, 1, 1);

        var line = new ScheduleLine(room, date);

        line.Room.Should().BeSameAs(room);
        line.Date.Should().Be(date);
    }

    [Fact]
    public void Constructor_ShouldRejectNullRoom()
    {
        Action act = () => new ScheduleLine(null!, new DateOnly(2030, 1, 1));

        act.Should().Throw<ArgumentNullException>().WithParameterName("room");
    }

    [Fact]
    public void AddShowing_ShouldAddItToRoom()
    {
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var line = new ScheduleLine(room, new DateOnly(2030, 1, 1));
        var showing = CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0));

        line.AddShowing(showing);

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
        line.GetShowing(showing.ShowingId).Should().BeSameAs(showing);
    }

    [Fact]
    public void AddShowing_ShouldRejectNull()
    {
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var line = new ScheduleLine(room, new DateOnly(2030, 1, 1));
        Action act = () => line.AddShowing(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("showing");
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void GetAllShowings_ShouldOnlyReturnShowingsForItsDate()
    {
        var first = CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0));
        var nextDay = CreateShowing(new DateTime(2030, 1, 2, 12, 0, 0));
        var room = new Room(Guid.NewGuid(), new[] { first, nextDay });
        var line = new ScheduleLine(room, new DateOnly(2030, 1, 1));

        line.GetAllShowings().Should().ContainSingle().Which.Should().BeSameAs(first);
    }

    [Fact]
    public void GetShowing_ShouldNotFindShowingsOnAnotherDate()
    {
        var showing = CreateShowing(new DateTime(2030, 1, 2, 12, 0, 0));
        var room = new Room(Guid.NewGuid(), new[] { showing });
        var line = new ScheduleLine(room, new DateOnly(2030, 1, 1));

        line.GetShowing(showing.ShowingId).Should().BeNull();
        line.GetShowing(Guid.NewGuid()).Should().BeNull();
    }

    private static Showing CreateShowing(DateTime start)
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        return new Showing(Guid.NewGuid(), film, start, 12.50m);
    }
}
