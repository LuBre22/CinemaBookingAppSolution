using System.Globalization;
using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;
using Reqnroll;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingAppAcceptanceTests.StepDefinitions;

[Binding]
[Scope(Feature = "Ticket booking")]
public sealed class TicketBookingSteps
{
    private readonly CinemaFixture _cinema = new();
    private readonly DateTime _future = DateTime.Today.AddDays(3).AddHours(12);
    private Showing _showing = null!;
    private CinemaBooking _booking = null!;
    private BookingResult _result = null!;
    private Exception? _error;
    private Guid _actor;
    private IReadOnlyList<CinemaBooking> _history = Array.Empty<CinemaBooking>();
    private readonly List<CinemaBooking> _expectedHistory = new();
    private readonly Dictionary<CinemaBooking, DateTime> _starts = new();

    public TicketBookingSteps() => _actor = _cinema.CustomerId;

    [Given("a future showtime is scheduled with available seats A1 and A2")]
    [Given("a future showtime is scheduled")]
    public void FutureShowing()
    {
        _showing = _cinema.ScheduleFuture(CinemaFixture.NewRoom(), _future);
        _showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        _showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Given("the ticket price is 12.50 EUR per seat")]
    public void TicketPrice() => _showing.TicketPrice.Should().Be(12.50m);

    [Given("a future showtime is scheduled with seat A1 already confirmed")]
    public void ShowingWithOccupiedSeat()
    {
        FutureShowing();
        _cinema.Book(_showing, Guid.NewGuid(), "A1");
    }

    [Given("a scheduled showtime has started")]
    public void StartedShowing()
    {
        _showing = _cinema.SeedHistorical(CinemaFixture.NewRoom(), DateTime.Now.AddMinutes(-10));
        _showing.Status.Should().Be(FilmStatusCategories.RUNNING);
    }

    [Given("a future showtime has a confirmed booking for seat A1")]
    public void ConfirmedBooking()
    {
        FutureShowing();
        _booking = _cinema.Book(_showing, _cinema.CustomerId, "A1");
    }

    [Given("another customer owns a confirmed booking for a future scheduled showtime")]
    public void OtherCustomerBooking()
    {
        FutureShowing();
        _booking = _cinema.Book(_showing, Guid.NewGuid(), "A1");
    }

    [Given("the customer has a cancelled booking")]
    public void CancelledBooking()
    {
        ConfirmedBooking();
        BookingTool.CancelBooking(_booking, _cinema.CustomerId, _cinema.Authenticator, _cinema.Payment);
        _booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
    }

    [Given("the customer has a confirmed booking for a showtime that has started")]
    public void BookingForStartedShowing()
    {
        StartedShowing();
        _booking = new CinemaBooking(_showing, _cinema.CustomerId, new[] { "A1" }, BookingStatusTypes.CONFIRMED);
        _cinema.Bookings.Add(_booking);
    }

    [Given("the customer has confirmed and cancelled bookings for multiple showtimes")]
    public void BookingsForMultipleShowings()
    {
        var room = CinemaFixture.NewRoom();
        var later = _cinema.ScheduleFuture(room, _future.AddDays(2));
        var earlier = _cinema.ScheduleFuture(room, _future);
        var middle = _cinema.ScheduleFuture(room, _future.AddDays(1));
        var laterBooking = _cinema.Book(later, _cinema.CustomerId, "A1");
        var earlierBooking = _cinema.Book(earlier, _cinema.CustomerId, "A1");
        var middleBooking = _cinema.Book(middle, _cinema.CustomerId, "A1");
        BookingTool.CancelBooking(middleBooking, _cinema.CustomerId, _cinema.Authenticator, _cinema.Payment);
        _cinema.Book(later, Guid.NewGuid(), "A2");
        _expectedHistory.AddRange(new[] { laterBooking, earlierBooking, middleBooking });
        _starts[laterBooking] = later.StartingTime;
        _starts[earlierBooking] = earlier.StartingTime;
        _starts[middleBooking] = middle.StartingTime;
    }

    [When("the customer books seats A1 and A2")]
    public void BookTwoSeats() => AttemptBooking(new[] { "A1", "A2" });

    [When("the customer books no seats")]
    public void BookNoSeats() => AttemptBooking(Array.Empty<string>());

    [When("the customer attempts to book seat {string}")]
    public void BookInvalidOrOccupiedSeat(string seat) => AttemptBooking(new[] { seat, "A2" });

    [When("the customer attempts to book an available seat")]
    public void BookAvailableSeat() => AttemptBooking(new[] { "A1" });

    [When("the customer cancels that booking")]
    [When("the customer attempts to cancel that booking")]
    [When("the customer attempts to cancel that booking again")]
    public void CancelBooking() =>
        _error = Record.Exception(() => BookingTool.CancelBooking(_booking, _actor, _cinema.Authenticator, _cinema.Payment));

    [When("the customer views their bookings")]
    public void ViewBookings() => _history = _cinema.Bookings.ViewMyBookings(_cinema.CustomerId);

    [Then("the booking is confirmed")]
    public void BookingConfirmed()
    {
        _result.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        _result.Booking.Should().BeSameAs(_booking);
    }

    [Then("the total price is 25.00 EUR")]
    public void TotalPrice() => _booking.Price.ToString(CultureInfo.InvariantCulture).Should().Be("25.00");

    [Then("seats A1 and A2 are no longer available for that showtime")]
    public void SeatsOccupied()
    {
        _showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        _showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Then("the booking is rejected")]
    public void BookingRejected() => _result.Status.Should().Be(BookingStatusTypes.DENIED);

    [Then("no booking is saved")]
    public void NoBookingSaved()
    {
        _result.Booking.Should().BeNull();
        _cinema.Bookings.ViewMyBookings(_cinema.CustomerId).Should().BeEmpty();
    }

    [Then("the customer's other selected seats remain unbooked")]
    public void OtherSeatsUnbooked() => _showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);

    [Then("the booking is cancelled")]
    public void BookingCancelled()
    {
        _error.Should().BeNull();
        _booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
    }

    [Then("seat A1 is available for that showtime")]
    public void SeatAvailable() => _showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);

    [Then("the cancellation is rejected as unauthorized")]
    public void Unauthorized() => _error.Should().BeOfType<UnauthorizedAccessException>();

    [Then("the cancellation is rejected")]
    public void CancellationRejected() => _error.Should().BeOfType<InvalidOperationException>();

    [Then("the booking remains confirmed")]
    public void BookingRemainsConfirmed() => _booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);

    [Then("the booking remains cancelled")]
    public void BookingRemainsCancelled() => _booking.Status.Should().Be(BookingStatusTypes.CANCELLED);

    [Then("all of those bookings are shown")]
    public void AllBookingsShown()
    {
        _history.Should().BeEquivalentTo(_expectedHistory);
        _history.Should().Contain(b => b.Status == BookingStatusTypes.CONFIRMED);
        _history.Should().Contain(b => b.Status == BookingStatusTypes.CANCELLED);
    }

    [Then("the bookings are ordered by showtime start")]
    public void BookingsOrdered() => _history.Select(booking => _starts[booking]).Should().BeInAscendingOrder();

    private void AttemptBooking(string[] seats)
    {
        _result = BookingTool.Receive(_cinema.Bookings, _cinema.Schedule, _showing.ShowingId, _cinema.CustomerId, seats);
        if (_result.Booking is not null)
        {
            _booking = _result.Booking;
            BookingTool.CommitBooking(_cinema.Schedule, _booking, _cinema.Payment);
        }
    }
}
