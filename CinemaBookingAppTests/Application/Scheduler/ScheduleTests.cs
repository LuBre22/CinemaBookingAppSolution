using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleTests
{
    [Fact]
    public void AddScheduleEntry_ShouldMakeLineAvailableByRoomAndDate()
    {
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var date = new DateOnly(2030, 1, 1);
        var line = new ScheduleLine(room, date);

        schedule.AddScheduleEntry(line);

        schedule.GetScheduleLine(room.RoomNumber, date).Should().BeSameAs(line);
        schedule.GetScheduleLine(Guid.NewGuid(), date).Should().BeNull();
        schedule.GetScheduleLine(room.RoomNumber, date.AddDays(1)).Should().BeNull();
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRemoveOnlySpecifiedLine()
    {
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var first = new ScheduleLine(room, new DateOnly(2030, 1, 1));
        var second = new ScheduleLine(room, new DateOnly(2030, 1, 2));
        schedule.AddScheduleEntry(first);
        schedule.AddScheduleEntry(second);

        schedule.RemoveScheduleEntry(first);

        schedule.ScheduleEntries.Should().ContainSingle().Which.Should().BeSameAs(second);
        schedule.GetScheduleLine(room.RoomNumber, first.Date).Should().BeNull();
    }

    [Fact]
    public void AddScheduleEntry_ShouldRejectNull()
    {
        var schedule = new Schedule();
        Action act = () => schedule.AddScheduleEntry(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("scheduleLine");
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRejectNull()
    {
        var schedule = new Schedule();
        Action act = () => schedule.RemoveScheduleEntry(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("scheduleLine");
    }

    [Fact]
    public void GetShowing_ShouldSearchAllScheduleLinesAndReturnNullForUnknownId()
    {
        var schedule = new Schedule();
        var firstRoom = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var secondRoom = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var firstLine = new ScheduleLine(firstRoom, new DateOnly(2030, 1, 1));
        var secondLine = new ScheduleLine(secondRoom, new DateOnly(2030, 1, 1));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var showing = new Showing(Guid.NewGuid(), film, new DateTime(2030, 1, 1, 12, 0, 0), 12.50m);
        secondLine.AddShowing(showing);
        schedule.AddScheduleEntry(firstLine);
        schedule.AddScheduleEntry(secondLine);

        schedule.GetShowing(showing.ShowingId).Should().BeSameAs(showing);
        schedule.GetShowing(Guid.NewGuid()).Should().BeNull();
    }
}
