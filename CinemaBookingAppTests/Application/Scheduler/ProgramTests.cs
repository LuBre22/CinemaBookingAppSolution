using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;
using CinemaProgram = CinemaBookingApp.Application.Scheduler.Program;

namespace CinemaBookingAppTests.Application.Scheduler;

public class ProgramTests
{
    [Fact]
    public void GetProgram_ShouldGroupFutureScheduledShowingsByFilmAndOrderEachGroupByStartTime()
    {
        var firstFilm = new Film(Guid.NewGuid(), "Same title", Genre.Drama, TimeSpan.FromMinutes(60));
        var secondFilm = new Film(Guid.NewGuid(), "Same title", Genre.Comedy, TimeSpan.FromMinutes(60));
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var otherRoom = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(80));
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var past = new Showing(Guid.NewGuid(), firstFilm, DateTime.Today.AddDays(-2), 10m, room);
        room.AddShowing(past);
        schedule.AddShowing(room, past);

        var firstLate = Guid.NewGuid();
        var firstEarly = Guid.NewGuid();
        var secondLate = Guid.NewGuid();
        var secondEarly = Guid.NewGuid();
        var cancelled = Guid.NewGuid();
        var catalog = new FilmCatalog();
        catalog.AddMovie(firstFilm);
        catalog.AddMovie(secondFilm);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, firstLate, firstFilm, start.AddHours(6), 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, secondEarly, secondFilm, start.AddHours(2), 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, otherRoom, secondLate, secondFilm, start.AddHours(4), 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, otherRoom, firstEarly, firstFilm, start, 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, cancelled, firstFilm, start.AddHours(8), 10m);
        CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, cancelled, new CinemaBookingApp.Domain.Payment.ExternalPaymentSystem(true));

        var result = CinemaProgram.GetProgram(
            schedule, DateTime.Today.AddDays(-3), start.AddDays(1));

        result.Should().HaveCount(4);
        result.Select(line => line.ShowingId).Should().NotContain(past.ShowingId).And.NotContain(cancelled);
        var ids = result.Select(line => line.ShowingId).ToArray();
        ids.Should().SatisfyRespectively(
            id => id.Should().Be(firstEarly),
            id => id.Should().Be(firstLate),
            id => id.Should().Be(secondEarly),
            id => id.Should().Be(secondLate));
        result.Select(line => line.FilmId).Should().Equal(
            firstFilm.MovieId, firstFilm.MovieId, secondFilm.MovieId, secondFilm.MovieId);
        var firstLine = result.Single(line => line.ShowingId == firstEarly);
        firstLine.Title.Should().Be(firstFilm.Title);
        firstLine.StartTime.Should().Be(start);
        firstLine.Duration.Should().Be(firstFilm.Duration);
        firstLine.Room.Should().BeSameAs(otherRoom);
        firstLine.AvailableSeats.Should().HaveCount(80)
            .And.OnlyContain(seat => seat.Value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void GetProgram_ShouldHonorInclusiveDateRange()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var before = new Showing(Guid.NewGuid(), film, start.AddHours(-2), 10m, room);
        var first = new Showing(Guid.NewGuid(), film, start, 10m, room);
        var last = new Showing(Guid.NewGuid(), film, start.AddHours(2), 10m, room);
        var after = new Showing(Guid.NewGuid(), film, start.AddHours(4), 10m, room);
        foreach (var showing in new[] { before, first, last, after })
            CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, showing.ShowingId, film, showing.StartingTime, showing.TicketPrice);

        var result = CinemaProgram.GetProgram(schedule, start, last.StartingTime);

        result.Select(line => line.ShowingId).Should().Equal(first.ShowingId, last.ShowingId);
    }

    [Fact]
    public void GetProgram_ShouldReturnEmptyWhenNoShowingsMatch()
    {
        CinemaProgram.GetProgram(new Schedule(), DateTime.Today, DateTime.Today.AddDays(1))
            .Should().BeEmpty();
    }

    [Fact]
    public void GetProgram_ShouldRejectNullSchedule()
    {
        Action act = () => CinemaProgram.GetProgram(null!, DateTime.Today, DateTime.Today.AddDays(1));

        act.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
    }

    [Fact]
    public void GetProgram_ShouldExcludeCancelledShowings()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var scheduledId = Guid.NewGuid();
        var removedLineId = Guid.NewGuid();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, scheduledId, film, start, 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, removedLineId, film, start.AddDays(1), 10m);
        CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, removedLineId, new CinemaBookingApp.Domain.Payment.ExternalPaymentSystem(true));

        var result = CinemaProgram.GetProgram(schedule, start, start.AddDays(3));

        result.Select(line => line.ShowingId).Should().Equal(scheduledId);
    }

    [Fact]
    public void GetProgram_ShouldExcludeInternallyAddedRoomShowingOnScheduledDate()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var scheduledId = Guid.NewGuid();
        var direct = new Showing(Guid.NewGuid(), film, start.AddHours(2), 10m, room);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, scheduledId, film, start, 10m);
        room.AddShowing(direct);

        var result = CinemaProgram.GetProgram(schedule, start, start.AddDays(1));

        result.Select(line => line.ShowingId).Should().Equal(scheduledId);
        schedule.GetShowing(direct.ShowingId).Should().BeNull();
    }

    [Fact]
    public void GetProgram_ShouldExcludeShowingsScheduledInAnotherSchedule()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var otherSchedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, firstId, film, start, 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, otherSchedule, room, secondId, film, start.AddHours(2), 10m);

        CinemaProgram.GetProgram(schedule, start, start.AddDays(1))
            .Select(line => line.ShowingId).Should().Equal(firstId);
        CinemaProgram.GetProgram(otherSchedule, start, start.AddDays(1))
            .Select(line => line.ShowingId).Should().Equal(secondId);
    }

    [Fact]
    public void GetProgram_ShouldRetainScheduledShowingWhenDirectRoomRemovalIsRejected()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var id = Guid.NewGuid();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, id, film, start, 10m);
        var showing = room.Showings.Single();

        Action act = () => room.RemoveShowing(id);

        act.Should().Throw<InvalidOperationException>();
        schedule.GetShowing(id).Should().BeSameAs(showing);
        CinemaProgram.GetProgram(schedule, start, start.AddDays(1))
            .Should().ContainSingle().Which.ShowingId.Should().Be(id);
    }

    [Fact]
    public void GetProgram_ShouldPreserveEachShowingsRoom()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var otherRoom = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(80));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, otherRoom, Guid.NewGuid(), film, start.AddHours(2), 10m);

        var result = CinemaProgram.GetProgram(schedule, start, start.AddDays(1));

        result.Select(line => line.Room).Should().Equal(room, otherRoom);
        result.Select(line => line.AvailableSeats.Count).Should().Equal(100, 80);
    }

    [Fact]
    public void GetProgram_ShouldOrderFilmShowingsAcrossScheduleLines()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var start = DateTime.Today.AddDays(3).AddHours(12);
        var early = Guid.NewGuid();
        var late = Guid.NewGuid();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, late, film, start.AddDays(1), 10m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, early, film, start, 10m);

        var result = CinemaProgram.GetProgram(schedule, start, start.AddDays(2));

        result.Select(line => line.ShowingId).Should().Equal(early, late);
    }
}
