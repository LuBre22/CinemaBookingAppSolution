using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleLineTests
{
    [Fact]
    public void DailyView_ShouldShowAllRoomsAndFilterByRoomAndDate()
    {
        var schedule = new Schedule();
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var firstRoom = Room.GetOrCreate(Guid.NewGuid());
        var secondRoom = Room.GetOrCreate(Guid.NewGuid());
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(90));
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var nextDayId = Guid.NewGuid();
        var line = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start));

        manager.AddShowing(schedule, firstRoom, firstId, film, start.AddHours(3), 12.50m);
        manager.AddShowing(schedule, secondRoom, secondId, film, start, 12.50m);
        manager.AddShowing(schedule, firstRoom, nextDayId, film, start.AddDays(1), 12.50m);

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
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var room = Room.GetOrCreate(Guid.NewGuid());
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(120));
        var start = DateTime.Today.AddDays(2).AddHours(23);
        var id = Guid.NewGuid();
        manager.AddShowing(schedule, room, id, film, start, 12.50m);

        var firstDay = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start));
        var secondDay = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start.AddDays(1)));

        firstDay.GetAllShowings().Should().ContainSingle().Which.ShowingId.Should().Be(id);
        secondDay.GetAllShowings().Should().BeEmpty();
    }
}
