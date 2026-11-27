using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ScheduleTests
{
    [Fact]
    public void ScheduleCollections_ShouldNotAllowExternalModification()
    {
        var (schedule, room, film, catalog, start) = CreateSetup();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        var line = schedule.ScheduleEntries.Single();
        var entries = (ICollection<ScheduleLine>)schedule.ScheduleEntries;
        var history = (ICollection<ScheduleLine>)schedule.CancelledEntries;

        Action remove = () => entries.Remove(line);
        Action add = () => history.Add(line);

        remove.Should().Throw<NotSupportedException>();
        add.Should().Throw<NotSupportedException>();
        schedule.ScheduleEntries.Should().ContainSingle().Which.Should().BeSameAs(line);
        schedule.CancelledEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldCreateOneLinePerShowingOnSameDay()
    {
        var (schedule, room, film, catalog, start) = CreateSetup();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, firstId, film, start, 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, secondId, film, start.AddHours(3), 10m);

        schedule.ScheduleEntries.Select(line => line.Showing.ShowingId).Should().Equal(firstId, secondId);
        schedule.GetShowing(firstId).Should().BeSameAs(schedule.ScheduleEntries[0].Showing);
        schedule.GetShowing(secondId).Should().BeSameAs(schedule.ScheduleEntries[1].Showing);
        schedule.GetShowing(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRejectPlannedShowingWithoutChangingAnything()
    {
        var (schedule, room, film, catalog, start) = CreateSetup();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        var line = schedule.ScheduleEntries.Single();

        Action act = () => schedule.RemoveScheduleEntry(line);

        act.Should().Throw<InvalidOperationException>();
        schedule.ScheduleEntries.Should().ContainSingle().Which.Should().BeSameAs(line);
        schedule.CancelledEntries.Should().BeEmpty();
        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(line.Showing);
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(-90)]
    public void RemoveScheduleEntry_ShouldRejectStartedShowings(int startOffsetMinutes)
    {
        var (schedule, room, film, _, _) = CreateSetup();
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Now.AddMinutes(startOffsetMinutes), 10m);
        room.AddShowing(showing);
        schedule.AddShowing(room, showing);
        var line = schedule.ScheduleEntries.Single();

        Action act = () => schedule.RemoveScheduleEntry(line);

        act.Should().Throw<InvalidOperationException>();
        schedule.ScheduleEntries.Should().ContainSingle();
        room.Showings.Should().ContainSingle();
        schedule.CancelledEntries.Should().BeEmpty();
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRemoveAndArchiveCancelledShowing()
    {
        var (schedule, room, film, catalog, start) = CreateSetup();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        var line = schedule.ScheduleEntries.Single();
        line.Showing.Cancel();

        schedule.RemoveScheduleEntry(line);

        schedule.ScheduleEntries.Should().BeEmpty();
        schedule.CancelledEntries.Should().ContainSingle().Which.Should().BeSameAs(line);
        room.Showings.Should().BeEmpty();
        line.Showing.Status.Should().Be(FilmStatusCategories.CANCELLED);
    }

    [Fact]
    public void RemoveScheduleEntry_ShouldRejectNullAndForeignLine()
    {
        var (schedule, room, film, catalog, start) = CreateSetup();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        var line = schedule.ScheduleEntries.Single();
        Action nullLine = () => schedule.RemoveScheduleEntry(null!);
        Action foreignLine = () => new Schedule().RemoveScheduleEntry(line);

        nullLine.Should().Throw<ArgumentNullException>().WithParameterName("scheduleLine");
        foreignLine.Should().Throw<ArgumentException>().WithParameterName("scheduleLine");
        schedule.ScheduleEntries.Should().ContainSingle();
        room.Showings.Should().ContainSingle();
    }

    private static (Schedule Schedule, Room Room, Film Film, FilmCatalog Catalog, DateTime Start) CreateSetup()
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        return (new Schedule(), Room.GetOrCreate(Guid.NewGuid(), 100), film, catalog, DateTime.Today.AddDays(3).AddHours(12));
    }
}
