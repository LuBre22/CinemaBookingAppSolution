using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;
using CinemaProgram = CinemaBookingApp.Application.Scheduler.Program;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;

namespace CinemaBookingAppTests.Application.ShowRooms;

public class SeatAvailabilityTests
{
    [Fact]
    public void Room_ShouldUseExplicitSeatIdsAndDeriveCapacity()
    {
        var ids = new List<string> { "A1", "A2", "Z99" };
        var room = Room.GetOrCreate(Guid.NewGuid(), ids);
        ids.Add("extra");

        room.Capacity.Should().Be(3);
        room.SeatIds.Should().Equal("A1", "A2", "Z99");
        Action mutate = () => ((ICollection<string>)room.SeatIds).Add("extra");
        mutate.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ShowingAndProgram_ShouldExposeAllSeatsAsAvailable()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2", "Z99" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        CinemaManager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);

        var showing = schedule.ScheduleEntries.Single().Showing;
        var line = CinemaProgram.GetProgram(schedule, start, start.AddDays(1)).Single();
        showing.Seats.Should().HaveCount(3).And.OnlyContain(seat => seat.Value == SeatAvailabilityTypes.AVAILABLE);
        showing.Seats.Keys.Should().BeEquivalentTo("A1", "A2", "Z99");
        line.AvailableSeats.Should().BeEquivalentTo(showing.Seats);
        Action mutate = () => ((IDictionary<string, SeatAvailabilityTypes>)showing.Seats)["A1"] = SeatAvailabilityTypes.OCCUPIED;
        mutate.Should().Throw<NotSupportedException>();
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Showings_ShouldHaveIndependentSeatMaps()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        CinemaManager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        CinemaManager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start.AddHours(2), 10m);

        schedule.ScheduleEntries[0].Showing.Seats.Should().NotBeSameAs(schedule.ScheduleEntries[1].Showing.Seats);
        schedule.ScheduleEntries[1].Showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Room_ShouldRejectLayoutChangesWhileShowingsExist()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        CinemaManager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, DateTime.Today.AddDays(2), 10m);

        Action update = () => Room.GetOrCreate(id, new[] { "A1", "A3" });

        update.Should().Throw<InvalidOperationException>();
        room.SeatIds.Should().Equal("A1", "A2");
        Room.GetOrCreate(id, new[] { "A2", "A1" }).Should().BeSameAs(room);
    }

    [Fact]
    public void Room_ShouldRejectNullAndDuplicateOrBlankSeatIds()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, new[] { "A1", "A2" });
        Action nullIds = () => Room.GetOrCreate(id, null!);
        Action duplicateIds = () => Room.GetOrCreate(id, new[] { "A1", "A1" });
        Action blankId = () => Room.GetOrCreate(id, new[] { "A1", " " });
        Action nullId = () => Room.GetOrCreate(id, new[] { "A1", null! });

        nullIds.Should().Throw<ArgumentNullException>().WithParameterName("seatIds");
        duplicateIds.Should().Throw<ArgumentException>().WithParameterName("seatIds");
        blankId.Should().Throw<ArgumentException>().WithParameterName("seatIds");
        nullId.Should().Throw<ArgumentException>().WithParameterName("seatIds");
        room.SeatIds.Should().Equal("A1", "A2");
    }

    [Fact]
    public void Room_ShouldPermitLayoutChangesAfterCancellationWithoutChangingHistory()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(2);
        var showingId = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, room, showingId, film, start, 10m);
        var original = schedule.ScheduleEntries.Single().Showing;

        CinemaManager.CancelShowing(schedule, showingId);
        Room.GetOrCreate(id, new[] { "B1" });
        CinemaManager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);

        room.Capacity.Should().Be(1);
        room.SeatIds.Should().Equal("B1");
        original.Seats.Keys.Should().BeEquivalentTo("A1", "A2");
        schedule.CancelledEntries.Single().Showing.Should().BeSameAs(original);
        schedule.ScheduleEntries.Single().Showing.Seats.Keys.Should().Equal("B1");
    }

    [Fact]
    public void Room_ShouldRejectShowingForAnotherRoomOrAnOutdatedLayout()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1" });
        var otherRoom = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Today.AddDays(2), 10m, room);
        Action wrongRoom = () => otherRoom.AddShowing(showing);

        wrongRoom.Should().Throw<ArgumentException>().WithParameterName("showing");
        Room.GetOrCreate(room.RoomNumber, new[] { "B1" });
        Action outdated = () => room.AddShowing(showing);
        outdated.Should().Throw<ArgumentException>().WithParameterName("showing");
        room.Showings.Should().BeEmpty();
        otherRoom.Showings.Should().BeEmpty();
    }
}
