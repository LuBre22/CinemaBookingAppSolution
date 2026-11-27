using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;
using Reqnroll;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;
using CinemaProgram = CinemaBookingApp.Application.Scheduler.Program;

namespace CinemaBookingAppAcceptanceTests.StepDefinitions;

[Binding]
[Scope(Feature = "Showtime programming")]
public sealed class ShowtimeProgrammingSteps
{
    private readonly CinemaFixture _cinema = new();
    private readonly DateTime _future = DateTime.Today.AddDays(3).AddHours(12);
    private Room _room = null!;
    private Film _selectedFilm = null!;
    private Showing _showing = null!;
    private Showing _existing = null!;
    private CinemaBooking _booking = null!;
    private DateTime _requested;
    private Guid _attemptId;
    private Exception? _error;
    private List<CinemaProgram.ProgramLine> _program = new();
    private readonly List<Guid> _futureIds = new();

    [Given("a film and a room exist")]
    public void FilmAndRoomExist()
    {
        _room = CinemaFixture.NewRoom();
        _selectedFilm = _cinema.Film;
        _requested = _future;
    }

    [Given("the current time is before the requested showtime")]
    public void RequestedTimeIsFuture() => _requested.Should().BeAfter(DateTime.Now);

    [Given("the requested showtime is at or before the current time")]
    public void RequestedTimeIsPast() => _requested = DateTime.Now.AddMinutes(-1);

    [Given("either the selected film or room does not exist")]
    public void UnknownFilm()
    {
        _room = CinemaFixture.NewRoom();
        _selectedFilm = new Film(Guid.NewGuid(), "Not in catalog", Genre.Drama, TimeSpan.FromMinutes(90));
        _requested = _future;
    }

    [Given("a scheduled showtime occupies the selected room")]
    [Given("a scheduled showtime occupies a room")]
    public void ExistingShowing()
    {
        FilmAndRoomExist();
        _existing = _cinema.ScheduleFuture(_room, _future);
    }

    [Given("its occupied interval includes the film duration and a 30-minute buffer")]
    public void OccupiedInterval()
    {
        _existing.EndingTime.Should().Be(_existing.StartingTime.Add(_existing.Film.Duration));
    }

    [Given("a cancelled showtime previously occupied the selected room")]
    public void CancelledShowing()
    {
        ExistingShowing();
        Manager.CancelShowing(_cinema.Schedule, _existing.ShowingId, _cinema.Payment);
        _existing.Status.Should().Be(FilmStatusCategories.CANCELLED);
    }

    [Given("a future showtime is scheduled")]
    public void FutureShowing()
    {
        FilmAndRoomExist();
        _showing = _cinema.ScheduleFuture(_room, _future);
        _booking = _cinema.Book(_showing, _cinema.CustomerId, "A1");
    }

    [Given("a scheduled showtime has started")]
    public void StartedShowing()
    {
        FilmAndRoomExist();
        _showing = _cinema.SeedHistorical(_room, DateTime.Now.AddMinutes(-10));
        _showing.Status.Should().Be(FilmStatusCategories.RUNNING);
    }

    [Given("the program contains future scheduled, past scheduled, and cancelled showtimes")]
    public void ProgramContainsShowings()
    {
        FilmAndRoomExist();
        var secondFilm = new Film(Guid.NewGuid(), "Moon", Genre.Drama, TimeSpan.FromMinutes(60));
        _cinema.Catalog.AddMovie(secondFilm);
        _futureIds.Add(_cinema.ScheduleFuture(_room, _future.AddDays(2)).ShowingId);
        _futureIds.Add(_cinema.ScheduleFuture(_room, _future).ShowingId);
        _futureIds.Add(_cinema.ScheduleFuture(CinemaFixture.NewRoom(), _future.AddDays(1), secondFilm).ShowingId);
        _futureIds.Add(_cinema.ScheduleFuture(CinemaFixture.NewRoom(), _future.AddDays(3), secondFilm).ShowingId);
        _cinema.SeedHistorical(CinemaFixture.NewRoom(), DateTime.Now.AddDays(-1));
        var cancelled = _cinema.ScheduleFuture(CinemaFixture.NewRoom(), _future.AddDays(4));
        Manager.CancelShowing(_cinema.Schedule, cancelled.ShowingId, _cinema.Payment);
    }

    [When(@"the manager schedules the film in that room at the requested time for ([\d.]+) EUR")]
    [When(@"the manager schedules the film in that room for ([\d.]+) EUR")]
    public void ScheduleAtPrice(decimal price) => TrySchedule(_room, _selectedFilm, _requested, price);

    [When("the manager schedules a showtime using that selection")]
    public void ScheduleUnknown() => TrySchedule(_room, _selectedFilm, _requested, 12.50m);

    [When("the manager schedules another showtime whose interval overlaps it")]
    public void ScheduleOverlap() => TrySchedule(_room, _selectedFilm, _existing.EndingTime.AddMinutes(29), 12.50m);

    [When("the manager schedules a showtime in a different room at the same time")]
    public void ScheduleDifferentRoom() => TrySchedule(CinemaFixture.NewRoom(), _selectedFilm, _existing.StartingTime, 12.50m);

    [When("the manager schedules an overlapping showtime in that room")]
    public void ScheduleOverCancelled() => TrySchedule(_room, _selectedFilm, _existing.StartingTime, 12.50m);

    [When("the manager cancels the showtime")]
    public void CancelShowing() =>
        _error = Record.Exception(() => Manager.CancelShowing(_cinema.Schedule, _showing.ShowingId, _cinema.Payment));

    [When("a customer browses the program")]
    public void Browse() => _program = CinemaProgram.GetProgram(_cinema.Schedule, DateTime.Today.AddDays(-2), _future.AddDays(5));

    [Then("the showtime is scheduled")]
    [Then("the new showtime is scheduled")]
    public void ShowtimeScheduled()
    {
        _error.Should().BeNull();
        _cinema.Schedule.GetShowing(_attemptId).Should().NotBeNull();
    }

    [Then("the showtime is rejected")]
    [Then("the new showtime is rejected")]
    [Then("the cancellation is rejected")]
    public void Rejected()
    {
        _error.Should().NotBeNull();
        (_error is ArgumentException or InvalidOperationException).Should().BeTrue();
    }

    [Then("no showtime is saved")]
    public void NoShowingSaved() => _cinema.Schedule.GetShowing(_attemptId).Should().BeNull();

    [Then("the existing showtime remains scheduled")]
    public void ExistingStillScheduled() => _cinema.Schedule.GetShowing(_existing.ShowingId).Should().BeSameAs(_existing);

    [Then("the showtime remains scheduled")]
    public void ShowingStillScheduled() => _cinema.Schedule.GetShowing(_showing.ShowingId).Should().BeSameAs(_showing);

    [Then("the showtime is cancelled")]
    public void ShowingCancelled()
    {
        _error.Should().BeNull();
        _showing.Status.Should().Be(FilmStatusCategories.CANCELLED);
        _cinema.Schedule.GetShowing(_showing.ShowingId).Should().BeNull();
    }

    [Then("its confirmed bookings are cancelled")]
    public void BookingsCancelled() => _booking.Status.Should().Be(BookingStatusTypes.CANCELLED);

    [Then("the showtime and bookings remain available for audit")]
    public void AuditAvailable()
    {
        _cinema.Schedule.CancelledEntries.Should().ContainSingle(line => ReferenceEquals(line.Showing, _showing));
        _cinema.Bookings.ViewMyBookings(_cinema.CustomerId).Should().ContainSingle(b => ReferenceEquals(b, _booking));
    }

    [Then("only future scheduled showtimes are shown")]
    public void OnlyFutureShowings()
    {
        _program.Select(line => line.ShowingId).Should().BeEquivalentTo(_futureIds);
        _program.Should().OnlyContain(line => line.StartTime > DateTime.Now);
    }

    [Then("showtimes are grouped by film")]
    public void GroupedByFilm()
    {
        var groups = _program.Select(line => line.FilmId).ToArray();
        groups.Distinct().Should().HaveCount(2);
        foreach (var id in groups.Distinct())
        {
            var positions = groups.Select((value, index) => (value, index)).Where(item => item.value == id).Select(item => item.index).ToArray();
            (positions.Last() - positions.First() + 1).Should().Be(positions.Length);
        }
    }

    [Then("showtimes in each group are ordered by start time")]
    public void SortedWithinGroups()
    {
        foreach (var group in _program.GroupBy(line => line.FilmId))
            group.Select(line => line.StartTime).Should().BeInAscendingOrder();
    }

    private void TrySchedule(Room room, Film film, DateTime start, decimal price)
    {
        _attemptId = Guid.NewGuid();
        _error = Record.Exception(() => Manager.AddShowing(_cinema.Catalog, _cinema.Schedule, room, _attemptId, film, start, price));
    }
}
