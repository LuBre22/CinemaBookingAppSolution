using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Manager;

public class ManagerTests
{
    [Fact]
    public void AddShowing_ShouldRejectFilmNotInCatalogWithoutChangingScheduleOrRoom()
    {
        var catalog = new FilmCatalog();
        var schedule = new Schedule();
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = CreateFilm();

        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(
            catalog, schedule, room, Guid.NewGuid(), film, DateTime.Now.AddDays(2), 10m);

        act.Should().Throw<ArgumentException>().WithParameterName("film");
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectRemovedFilm()
    {
        var catalog = new FilmCatalog();
        var film = CreateFilm();
        catalog.AddMovie(film);
        catalog.RemoveMovie(film.MovieId);
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var schedule = new Schedule();

        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(
            catalog, schedule, room, Guid.NewGuid(), film, DateTime.Now.AddDays(2), 10m);

        act.Should().Throw<ArgumentException>().WithParameterName("film");
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldUseCatalogFilmWhenSameIdHasDifferentDetails()
    {
        var catalog = new FilmCatalog();
        var film = CreateFilm();
        catalog.AddMovie(film);
        var supplied = new Film(film.MovieId, "Different", Genre.Drama, TimeSpan.FromMinutes(30));
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);

        CinemaBookingApp.Application.Actors.Manager.AddShowing(
            catalog, new Schedule(), room, Guid.NewGuid(), supplied, DateTime.Now.AddDays(2), 10m);

        room.Showings.Should().ContainSingle().Which.Film.Should().BeSameAs(film);
    }

    [Fact]
    public void AddShowing_ShouldRejectNullCatalog()
    {
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(
            null!, new Schedule(), Room.GetOrCreate(Guid.NewGuid(), 100),
            Guid.NewGuid(), CreateFilm(), DateTime.Now.AddDays(2), 10m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("catalog");
    }

    [Fact]
    public void CreateFilm_ShouldAddFilmWithProvidedDetailsToCatalog()
    {
        var catalog = new FilmCatalog();
        CinemaBookingApp.Application.Actors.Manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

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
        CinemaBookingApp.Application.Actors.Manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        CinemaBookingApp.Application.Actors.Manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        var ids = catalog.GetAllMovies().Select(film => film.MovieId).ToArray();
        ids.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        ids.Should().NotContain(Guid.Empty);
    }

    [Fact]
    public void CreateFilm_ShouldRejectNullCatalog()
    {
        Action act = () => CinemaBookingApp.Application.Actors.Manager.CreateFilm(null!, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        act.Should().Throw<ArgumentNullException>().WithParameterName("catalog");
    }

    [Fact]
    public void CreateFilm_ShouldRejectEmptyTitleWithoutAddingFilm()
    {
        var catalog = new FilmCatalog();

        Action act = () => CinemaBookingApp.Application.Actors.Manager.CreateFilm(catalog, "", Genre.Drama, TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Fact]
    public void CreateFilm_ShouldRejectUndefinedGenreWithoutAddingFilm()
    {
        var catalog = new FilmCatalog();

        Action act = () => CinemaBookingApp.Application.Actors.Manager.CreateFilm(catalog, "Arrival", (Genre)(-1), TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateFilm_ShouldRejectNonPositiveDurationWithoutAddingFilm(int minutes)
    {
        var catalog = new FilmCatalog();

        Action act = () => CinemaBookingApp.Application.Actors.Manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(minutes));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldCreateScheduleLineAndAddShowingOnce()
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var id = Guid.NewGuid();

        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, id, film, start, 12.50m);

        var line = schedule.GetScheduleLine(DateOnly.FromDateTime(start));
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
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 12.50m);

        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start.AddMinutes(minutes), 12.50m);

        room.Showings.Should().HaveCount(2);
        schedule.ScheduleEntries.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-149)]
    [InlineData(0)]
    [InlineData(149)]
    public void AddShowing_ShouldRejectOverlapOrInsufficientBuffer(int minutes)
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 12.50m);
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start.AddMinutes(minutes), 12.50m);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().ContainSingle();
        schedule.ScheduleEntries.Should().ContainSingle();
    }

    [Fact]
    public void AddShowing_ShouldAllowSameTimeInDifferentRooms()
    {
        var schedule = new Schedule();
        var firstRoom = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var secondRoom = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);

        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, firstRoom, Guid.NewGuid(), film, start, 12.50m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, secondRoom, Guid.NewGuid(), film, start, 12.50m);

        firstRoom.Showings.Should().ContainSingle();
        secondRoom.Showings.Should().ContainSingle();
        schedule.ScheduleEntries.Should().ContainSingle();
        var line = schedule.GetScheduleLine(DateOnly.FromDateTime(start));
        line!.GetAllShowings().Should().HaveCount(2);
        line.GetShowingsForRoom(firstRoom.RoomNumber).Should().ContainSingle();
        line.GetShowingsForRoom(secondRoom.RoomNumber).Should().ContainSingle();
    }

    [Fact]
    public void AddShowing_ShouldCheckBufferAcrossMidnight()
    {
        var schedule = new Schedule();
        var roomNumber = Guid.NewGuid();
        var firstDayRoom = CreateRoom(roomNumber, Array.Empty<Showing>());
        var secondDayRoom = CreateRoom(roomNumber, Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(23);
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, firstDayRoom, Guid.NewGuid(), film, start, 12.50m);
        Action overlap = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, secondDayRoom, Guid.NewGuid(), film, start.AddMinutes(149), 12.50m);

        overlap.Should().Throw<InvalidOperationException>();
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, secondDayRoom, Guid.NewGuid(), film, start.AddMinutes(150), 12.50m);

        firstDayRoom.Should().BeSameAs(secondDayRoom);
        firstDayRoom.Showings.Should().HaveCount(2);
        schedule.ScheduleEntries.Should().HaveCount(2);
    }

    [Fact]
    public void AddShowing_ShouldCheckPreviousDayWhenNextDayIsScheduledFirst()
    {
        var schedule = new Schedule();
        var roomNumber = Guid.NewGuid();
        var firstDayRoom = CreateRoom(roomNumber, Array.Empty<Showing>());
        var secondDayRoom = CreateRoom(roomNumber, Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(23);
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, secondDayRoom, Guid.NewGuid(), film, start.AddMinutes(149), 12.50m);
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, firstDayRoom, Guid.NewGuid(), film, start, 12.50m);

        act.Should().Throw<InvalidOperationException>();
        firstDayRoom.Should().BeSameAs(secondDayRoom);
        firstDayRoom.Showings.Should().ContainSingle();
    }

    [Fact]
    public void AddShowing_ShouldShareRoomAcrossDatesWhileKeepingDailyViewsSeparate()
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, firstId, film, start, 12.50m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, Room.GetOrCreate(room.RoomNumber), secondId, film, start.AddDays(1), 12.50m);

        var firstLine = schedule.GetScheduleLine(DateOnly.FromDateTime(start));
        var secondLine = schedule.GetScheduleLine(DateOnly.FromDateTime(start.AddDays(1)));
        firstLine.Should().NotBeNull();
        secondLine.Should().NotBeNull();
        secondLine.Should().NotBeNull();
        room.Showings.Should().HaveCount(2);
        firstLine.GetAllShowings().Should().ContainSingle().Which.ShowingId.Should().Be(firstId);
        secondLine.GetAllShowings().Should().ContainSingle().Which.ShowingId.Should().Be(secondId);

        CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, firstId);

        firstLine.GetAllShowings().Should().BeEmpty();
        secondLine.GetAllShowings().Should().ContainSingle().Which.ShowingId.Should().Be(secondId);
        schedule.GetShowing(secondId).Should().NotBeNull();
    }

    [Fact]
    public void AddShowing_ShouldRejectNullRoom()
    {
        var catalog = new FilmCatalog();
        var film = CreateFilm();
        catalog.AddMovie(film);
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, new Schedule(), null!, Guid.NewGuid(), film, DateTime.Now.AddDays(2), 12.50m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("room");
    }

    [Fact]
    public void AddShowing_ShouldRejectNullSchedule()
    {
        var catalog = new FilmCatalog();
        var film = CreateFilm();
        catalog.AddMovie(film);
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, null!, Room.GetOrCreate(Guid.NewGuid(), 100), Guid.NewGuid(), film, DateTime.Now.AddDays(2), 12.50m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
    }

    [Fact]
    public void CancelShowing_ShouldRejectNullSchedule()
    {
        Action act = () => CinemaBookingApp.Application.Actors.Manager.CancelShowing(null!, Guid.NewGuid());

        act.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
    }

    [Fact]
    public void AddShowing_ShouldRejectNullFilmWithoutSavingShowing()
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var catalog = new FilmCatalog();
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), null!, DateTime.Now.AddDays(2), 12.50m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("film");
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectPastShowingInEmptyRoom()
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var catalog = new FilmCatalog();
        var film = CreateFilm();
        catalog.AddMovie(film);
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, DateTime.Now.AddDays(-1), 12.50m);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectInvalidPriceWithoutSavingShowing()
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var catalog = new FilmCatalog();
        var film = CreateFilm();
        catalog.AddMovie(film);
        Action act = () => CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, DateTime.Now.AddDays(2), 0m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("ticketPrice");
        room.Showings.Should().BeEmpty();
        schedule.ScheduleEntries.Should().BeEmpty();
    }

    [Fact]
    public void CancelShowing_ShouldFreeRoomForAnotherShowing()
    {
        var schedule = new Schedule();
        var room = CreateRoom(Guid.NewGuid(), Array.Empty<Showing>());
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var id = Guid.NewGuid();
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, id, film, start, 12.50m);

        CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, id);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 12.50m);

        room.Showings.Should().ContainSingle();
        room.Showings!.Single().ShowingId.Should().NotBe(id);
    }

    [Fact]
    public void CancelShowing_ShouldRejectUnknownShowing()
    {
        Action act = () => CinemaBookingApp.Application.Actors.Manager.CancelShowing(new Schedule(), Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CancelShowing_ShouldRemoveOnlyTheMatchingShowingFromItsRoom()
    {
        var schedule = new Schedule();
        var firstRoom = Room.GetOrCreate(Guid.NewGuid(), 100);
        var secondRoom = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, firstRoom, firstId, film, start, 12.50m);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, secondRoom, secondId, film, start, 12.50m);

        CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, secondId);

        firstRoom.Showings.Should().ContainSingle().Which.ShowingId.Should().Be(firstId);
        secondRoom.Showings.Should().BeEmpty();
        schedule.GetScheduleLine(DateOnly.FromDateTime(start))!.GetAllShowings()
            .Should().ContainSingle().Which.ShowingId.Should().Be(firstId);
    }

    [Fact]
    public void CancelShowing_ShouldRejectShowingThatHasStarted()
    {
        var schedule = new Schedule();
        var start = DateTime.Now.AddDays(-1);
        var showing = new Showing(Guid.NewGuid(), CreateFilm(), start, 12.50m);
        var room = CreateRoom(Guid.NewGuid(), new[] { showing });
        var line = schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(start));
        line.AddShowing(room, showing);
        line.GetShowing(showing.ShowingId).Should().NotBeNull();
        Action act = () => CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, showing.ShowingId);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().Contain(showing);
        line.GetShowing(showing.ShowingId).Should().BeSameAs(showing);
    }

    [Fact]
    public void CancelShowing_ShouldRejectShowingNotExplicitlyScheduled()
    {
        var schedule = new Schedule();
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, Guid.NewGuid(), film, start, 10m);
        var direct = new Showing(Guid.NewGuid(), film, start.AddHours(3), 10m);
        room.AddShowing(direct);

        Action act = () => CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, direct.ShowingId);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().Contain(direct);
        schedule.GetShowing(direct.ShowingId).Should().BeNull();
    }

    [Fact]
    public void CancelShowing_ShouldRemoveScheduledInstanceWhenRoomHasAnotherShowingWithSameId()
    {
        var schedule = new Schedule();
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var film = CreateFilm();
        var start = DateTime.Today.AddDays(2).AddHours(12);
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var id = Guid.NewGuid();
        var direct = new Showing(id, film, start, 10m);
        room.AddShowing(direct);
        CinemaBookingApp.Application.Actors.Manager.AddShowing(catalog, schedule, room, id, film, start.AddHours(3), 10m);

        CinemaBookingApp.Application.Actors.Manager.CancelShowing(schedule, id);

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(direct);
        schedule.GetShowing(id).Should().BeNull();
    }

    private static Film CreateFilm()
    {
        return new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(120));
    }

    private static Room CreateRoom(Guid id, IEnumerable<Showing> showings)
    {
        var room = Room.GetOrCreate(id, 100);
        foreach (var showing in showings)
            room.AddShowing(showing);
        return room;
    }
}
