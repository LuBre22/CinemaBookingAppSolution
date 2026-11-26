using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleTests
{
    [Fact]
    public void AddShowing_ShouldNotListShowingTwiceWhenRoomIsUsedRepeatedly()
    {
        var schedule = new Schedule();
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var room = Room.GetOrCreate(Guid.NewGuid());
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(60));
        var start = DateTime.Today.AddDays(3).AddHours(10);
        manager.AddShowing(schedule, room, Guid.NewGuid(), film, start, 10m);
        manager.AddShowing(schedule, room, Guid.NewGuid(), film, start.AddHours(3), 10m);

        schedule.GetScheduleLine(DateOnly.FromDateTime(start))!.GetAllShowings().Should().HaveCount(2);
    }

    [Fact]
    public void ScheduleEntries_ShouldBeEmptyForNewSchedule()
    {
        new Schedule().ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void GetOrCreateScheduleLine_ShouldReturnOneLinePerDate()
    {
        var schedule = new Schedule();
        var date = new DateOnly(2030, 1, 1);
        var first = schedule.GetOrCreateScheduleLine(date);
        var second = schedule.GetOrCreateScheduleLine(date.AddDays(1));

        schedule.GetOrCreateScheduleLine(date).Should().BeSameAs(first);
        schedule.GetScheduleLine(date).Should().BeSameAs(first);
        schedule.GetScheduleLine(date.AddDays(1)).Should().BeSameAs(second);
        schedule.GetScheduleLine(date.AddDays(2)).Should().BeNull();
        schedule.ScheduleEntries.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRemoveOnlySpecifiedLine()
    {
        var schedule = new Schedule();
        var first = schedule.GetOrCreateScheduleLine(new DateOnly(2030, 1, 1));
        var second = schedule.GetOrCreateScheduleLine(new DateOnly(2030, 1, 2));

        schedule.RemoveScheduleEntry(first);

        schedule.ScheduleEntries.Should().ContainSingle().Which.Should().BeSameAs(second);
        schedule.GetScheduleLine(first.Date).Should().BeNull();
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRejectNull()
    {
        var schedule = new Schedule();
        Action act = () => schedule.RemoveScheduleEntry(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("scheduleLine");
    }

    [Fact]
    public void GetShowing_ShouldSearchAllDatesAndReturnNullForUnknownId()
    {
        var schedule = new Schedule();
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var room = Room.GetOrCreate(Guid.NewGuid());
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        manager.AddShowing(schedule, room, firstId, film, start, 12.50m);
        manager.AddShowing(schedule, room, secondId, film, start.AddDays(1), 12.50m);

        schedule.GetShowing(firstId)!.ShowingId.Should().Be(firstId);
        schedule.GetShowing(secondId)!.ShowingId.Should().Be(secondId);
        schedule.GetShowing(Guid.NewGuid()).Should().BeNull();
    }
}
