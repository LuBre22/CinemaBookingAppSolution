using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleLineTests
{
    [Fact]
    public void ScheduleLine_ShouldRejectNullArguments()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Today.AddDays(2), 10m);

        Action nullRoom = () => new ScheduleLine(null!, showing);
        Action nullShowing = () => new ScheduleLine(room, null!);

        nullRoom.Should().Throw<ArgumentNullException>().WithParameterName("room");
        nullShowing.Should().Throw<ArgumentNullException>().WithParameterName("showing");
    }

    [Fact]
    public void ScheduleLine_ShouldRepresentOneShowingAndItsRoom()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(120));
        var start = DateTime.Today.AddDays(2).AddHours(23);
        var showing = new Showing(Guid.NewGuid(), film, start, 10m);
        room.AddShowing(showing);

        var line = new ScheduleLine(room, showing);

        line.Room.Should().BeSameAs(room);
        line.Showing.Should().BeSameAs(showing);
        line.Date.Should().Be(DateOnly.FromDateTime(start));
    }

    [Fact]
    public void ScheduleLine_ShouldRejectShowingNotInRoom()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Today.AddDays(2), 10m);

        Action act = () => new ScheduleLine(room, showing);

        act.Should().Throw<ArgumentException>().WithParameterName("showing");
    }
}
