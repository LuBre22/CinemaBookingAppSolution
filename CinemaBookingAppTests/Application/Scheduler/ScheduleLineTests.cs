using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleLineTests
{
    [Fact]
    public void AddShowing_ShouldRejectShowingOnAnotherDate()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var showing = new Showing(Guid.NewGuid(), film, start, 10m);
        room.AddShowing(showing);
        var line = new Schedule().GetOrCreateScheduleLine(DateOnly.FromDateTime(start.AddDays(1)));

        Action act = () => line.AddShowing(room, showing);

        act.Should().Throw<ArgumentException>();
        line.GetAllShowings().Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectShowingNotInRoom()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var showing = new Showing(Guid.NewGuid(), film, start, 10m);
        var line = new Schedule().GetOrCreateScheduleLine(DateOnly.FromDateTime(start));

        Action act = () => line.AddShowing(room, showing);

        act.Should().Throw<ArgumentException>();
        line.GetAllShowings().Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectDuplicateShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var showing = new Showing(Guid.NewGuid(), film, start, 10m);
        room.AddShowing(showing);
        var line = new Schedule().GetOrCreateScheduleLine(DateOnly.FromDateTime(start));
        line.AddShowing(room, showing);

        Action act = () => line.AddShowing(room, showing);

        act.Should().Throw<InvalidOperationException>();
        line.GetAllShowings().Should().ContainSingle();
    }

    [Fact]
    public void DailyView_ShouldShowAllRoomsAndFilterByRoomAndDate()
    {
        var schedule = new Schedule();
        var firstRoom = Room.GetOrCreate(Guid.NewGuid(), 100);
        var secondRoom = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(90));
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var nextDayId = Guid.NewGuid();
        var line = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start));

        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, firstRoom, firstId, film, start.AddHours(3), 12.50m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, secondRoom, secondId, film, start, 12.50m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, firstRoom, nextDayId, film, start.AddDays(1), 12.50m);

        line.Date.Should().Be(DateOnly.FromDateTime(start));
        line.GetAllShowings().Select(showing => showing.ShowingId).Should().Equal(secondId, firstId);
        line.GetShowingsForRoom(firstRoom.RoomNumber).Should().ContainSingle()
            .Which.ShowingId.Should().Be(firstId);
        line.GetShowingsForRoom(secondRoom.RoomNumber).Should().ContainSingle()
            .Which.ShowingId.Should().Be(secondId);
        line.GetShowingsForRoom(Guid.NewGuid()).Should().BeEmpty();
        line.GetShowing(secondId).Should().NotBeNull();
        line.GetShowing(nextDayId).Should().BeNull();
        line.GetShowing(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void DailyView_ShouldIncludeShowingOnStartDateOnlyEvenWhenItEndsNextDay()
    {
        var schedule = new Schedule();
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(120));
        var start = DateTime.Today.AddDays(2).AddHours(23);
        var id = Guid.NewGuid();
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, id, film, start, 12.50m);

        var firstDay = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start));
        var secondDay = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start.AddDays(1)));

        firstDay.GetAllShowings().Should().ContainSingle().Which.ShowingId.Should().Be(id);
        secondDay.GetAllShowings().Should().BeEmpty();
    }
}
