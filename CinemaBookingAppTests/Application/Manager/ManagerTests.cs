using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Manager;

public class ManagerTests
{
    [Fact]
    public void CreateFilm_ShouldAddFilmWithProvidedDetailsToCatalog()
    {
        var catalog = new FilmCatalog();
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        var film = catalog.GetAllMovies().Should().ContainSingle().Subject;
        film.MovieId.Should().NotBe(Guid.Empty);
        film.Title.Should().Be("Arrival");
        film.Genre.Should().Be(Genre.ScienceFiction);
        film.Duration.Should().Be(TimeSpan.FromMinutes(116));
    }

    [Fact]
    public void CreateFilm_ShouldAddFilmsWithDifferentIdentifiers()
    {
        var catalog = new FilmCatalog();
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        var ids = catalog.GetAllMovies().Select(film => film.MovieId).ToArray();
        ids.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        ids.Should().NotContain(Guid.Empty);
    }

    [Fact]
    public void CreateFilm_ShouldRejectEmptyTitleWithoutAddingFilm()
    {
        var catalog = new FilmCatalog();
        var manager = new CinemaBookingApp.Application.Actors.Manager();

        Action act = () => manager.CreateFilm(catalog, "", Genre.Drama, TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Fact]
    public void CreateFilm_ShouldRejectUndefinedGenreWithoutAddingFilm()
    {
        var catalog = new FilmCatalog();
        var manager = new CinemaBookingApp.Application.Actors.Manager();

        Action act = () => manager.CreateFilm(catalog, "Arrival", (Genre)(-1), TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateFilm_ShouldRejectNonPositiveDurationWithoutAddingFilm(int minutes)
    {
        var catalog = new FilmCatalog();
        var manager = new CinemaBookingApp.Application.Actors.Manager();

        Action act = () => manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(minutes));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldCreateScheduleLineAndAddShowingOnce()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var id = Guid.NewGuid();

        manager.AddShowing(schedule, room, id, film, start, 12.50m);

        var line = schedule.GetScheduleLine(room.RoomNumber, DateOnly.FromDateTime(start));
        line.Should().NotBeNull();
        var showing = room.Showings.Should().ContainSingle().Subject;
        showing.ShowingId.Should().Be(id);
        showing.Film.Should().BeSameAs(film);
        showing.StartingTime.Should().Be(start);
        showing.TicketPrice.Should().Be(12.50m);
        line!.GetShowing(id).Should().BeSameAs(showing);
        schedule.ScheduleEntries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-150)]
    [InlineData(150)]
    public void AddShowing_ShouldAllowThirtyMinuteBufferBeforeOrAfterExistingShowing(int minutes)
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        manager.AddShowing(schedule, room, Guid.NewGuid(), film, start, 12.50m);

        manager.AddShowing(schedule, room, Guid.NewGuid(), film, start.AddMinutes(minutes), 12.50m);

        room.Showings.Should().HaveCount(2);
        schedule.ScheduleEntries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-149)]
    [InlineData(0)]
    [InlineData(149)]
    public void AddShowing_ShouldRejectOverlapOrInsufficientBuffer(int minutes)
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        manager.AddShowing(schedule, room, Guid.NewGuid(), film, start, 12.50m);
        Action act = () => manager.AddShowing(schedule, room, Guid.NewGuid(), film, start.AddMinutes(minutes), 12.50m);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().ContainSingle();
        schedule.ScheduleEntries.Should().ContainSingle();
    }

    [Fact]
    public void AddShowing_ShouldAllowSameTimeInDifferentRooms()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var firstRoom = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var secondRoom = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);

        manager.AddShowing(schedule, firstRoom, Guid.NewGuid(), film, start, 12.50m);
        manager.AddShowing(schedule, secondRoom, Guid.NewGuid(), film, start, 12.50m);

        firstRoom.Showings.Should().ContainSingle();
        secondRoom.Showings.Should().ContainSingle();
        schedule.ScheduleEntries.Should().HaveCount(2);
    }

    [Fact]
    public void AddShowing_ShouldCheckBufferAcrossMidnight()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var roomNumber = Guid.NewGuid();
        var firstDayRoom = new Room(roomNumber, Array.Empty<Showing>());
        var secondDayRoom = new Room(roomNumber, Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(23);
        manager.AddShowing(schedule, firstDayRoom, Guid.NewGuid(), film, start, 12.50m);
        Action overlap = () => manager.AddShowing(schedule, secondDayRoom, Guid.NewGuid(), film, start.AddMinutes(149), 12.50m);

        overlap.Should().Throw<InvalidOperationException>();
        manager.AddShowing(schedule, secondDayRoom, Guid.NewGuid(), film, start.AddMinutes(150), 12.50m);

        firstDayRoom.Showings.Should().ContainSingle();
        secondDayRoom.Showings.Should().ContainSingle();
        schedule.ScheduleEntries.Should().HaveCount(2);
    }

    [Fact]
    public void AddShowing_ShouldCheckPreviousDayWhenNextDayIsScheduledFirst()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var roomNumber = Guid.NewGuid();
        var firstDayRoom = new Room(roomNumber, Array.Empty<Showing>());
        var secondDayRoom = new Room(roomNumber, Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(23);
        manager.AddShowing(schedule, secondDayRoom, Guid.NewGuid(), film, start.AddMinutes(149), 12.50m);
        Action act = () => manager.AddShowing(schedule, firstDayRoom, Guid.NewGuid(), film, start, 12.50m);

        act.Should().Throw<InvalidOperationException>();
        firstDayRoom.Showings.Should().BeEmpty();
        secondDayRoom.Showings.Should().ContainSingle();
    }

    [Fact]
    public void AddShowing_ShouldRejectNullRoom()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        Action act = () => manager.AddShowing(new Schedule(), null!, Guid.NewGuid(), CreateFilm(), DateTime.Now.AddDays(2), 12.50m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("room");
    }

    [Fact]
    public void AddShowing_ShouldRejectNullFilmWithoutSavingShowing()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        Action act = () => manager.AddShowing(schedule, room, Guid.NewGuid(), null!, DateTime.Now.AddDays(2), 12.50m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("film");
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectPastShowingInEmptyRoom()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        Action act = () => manager.AddShowing(schedule, room, Guid.NewGuid(), CreateFilm(), DateTime.Now.AddDays(-1), 12.50m);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectInvalidPriceWithoutSavingShowing()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        Action act = () => manager.AddShowing(schedule, room, Guid.NewGuid(), CreateFilm(), DateTime.Now.AddDays(2), 0m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("ticketPrice");
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void CancelShowing_ShouldFreeRoomForAnotherShowing()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var id = Guid.NewGuid();
        manager.AddShowing(schedule, room, id, film, start, 12.50m);

        manager.CancelShowing(schedule, id);
        manager.AddShowing(schedule, room, Guid.NewGuid(), film, start, 12.50m);

        room.Showings.Should().ContainSingle();
        room.Showings!.Single().ShowingId.Should().NotBe(id);
    }

    [Fact]
    public void CancelShowing_ShouldRejectUnknownShowing()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        Action act = () => manager.CancelShowing(new Schedule(), Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CancelShowing_ShouldRejectShowingThatHasStarted()
    {
        var manager = new CinemaBookingApp.Application.Actors.Manager();
        var schedule = new Schedule();
        var start = DateTime.Now.AddDays(-1);
        var showing = new Showing(Guid.NewGuid(), CreateFilm(), start, 12.50m);
        var room = new Room(Guid.NewGuid(), new[] { showing });
        schedule.AddScheduleEntry(new ScheduleLine(room, DateOnly.FromDateTime(start)));
        Action act = () => manager.CancelShowing(schedule, showing.ShowingId);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    private static Film CreateFilm()
    {
        return new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(120));
    }
}
