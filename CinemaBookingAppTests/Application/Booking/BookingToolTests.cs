using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;

namespace CinemaBookingAppTests.Application.Booking;

public class BookingToolTests
{
    [Fact]
    public void Receive_ShouldCreateReceivedBookingWithoutOccupyingSeats()
    {
        var (schedule, showing, customerId) = CreateScheduledShowing();
        var seats = new List<string> { "A1", "A2" };

        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, customerId, seats);
        seats.Clear();

        result.Status.Should().Be(BookingStatusTypes.RECEIVED);
        result.DenialReason.Should().BeNull();
        result.Booking.Should().NotBeNull();
        result.Booking!.ShowingId.Should().Be(showing.ShowingId);
        result.Booking.CustomerId.Should().Be(customerId);
        result.Booking.SeatNumbers.Should().Equal("A1", "A2");
        result.Booking.Price.Should().Be(25m);
        result.Booking.Status.Should().Be(BookingStatusTypes.RECEIVED);
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Receive_ShouldDenyUnknownShowing()
    {
        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), new Schedule(), Guid.NewGuid(), Guid.NewGuid(), new[] { "A1" });

        result.Status.Should().Be(BookingStatusTypes.DENIED);
        result.DenialReason.Should().Be(BookingDenialReason.ShowingNotFound);
        result.Booking.Should().BeNull();
    }

    [Fact]
    public void Receive_ShouldDenyCancelledShowing()
    {
        var (schedule, showing, customerId) = CreateScheduledShowing();
        CinemaManager.CancelShowing(schedule, showing.ShowingId, new CinemaBookingApp.Domain.Payment.ExternalPaymentSystem(true));

        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, customerId, new[] { "A1" });

        result.Status.Should().Be(BookingStatusTypes.DENIED);
        result.DenialReason.Should().Be(BookingDenialReason.ShowingNotFound);
        result.Booking.Should().BeNull();
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(-180)]
    public void Receive_ShouldDenyStartedShowing(int offsetMinutes)
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Now.AddMinutes(offsetMinutes), 12.50m, room);
        var schedule = new Schedule();
        room.AddShowing(showing);
        schedule.AddShowing(room, showing);

        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" });

        result.Status.Should().Be(BookingStatusTypes.DENIED);
        result.DenialReason.Should().Be(BookingDenialReason.ShowingNotBookable);
        result.Booking.Should().BeNull();
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Receive_ShouldDenyMissingAndEmptySeats()
    {
        var (schedule, showing, customerId) = CreateScheduledShowing();

        var missing = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, customerId, null);
        var empty = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, customerId, Array.Empty<string>());

        foreach (var result in new[] { missing, empty })
        {
            result.Status.Should().Be(BookingStatusTypes.DENIED);
            result.DenialReason.Should().Be(BookingDenialReason.NoSeatsSelected);
            result.Booking.Should().BeNull();
        }
    }

    [Theory]
    [InlineData("Z99")]
    [InlineData("")]
    [InlineData(null)]
    public void Receive_ShouldDenyInvalidSeatWithoutChangingAvailability(string? invalidSeat)
    {
        var (schedule, showing, customerId) = CreateScheduledShowing();

        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, customerId, new[] { "A1", invalidSeat! });

        result.Status.Should().Be(BookingStatusTypes.DENIED);
        result.DenialReason.Should().Be(BookingDenialReason.InvalidOrUnavailableSeat);
        result.Booking.Should().BeNull();
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Receive_ShouldDenyRepeatedSeat()
    {
        var (schedule, showing, customerId) = CreateScheduledShowing();

        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, customerId, new[] { "A1", "A1" });

        result.Status.Should().Be(BookingStatusTypes.DENIED);
        result.DenialReason.Should().Be(BookingDenialReason.InvalidOrUnavailableSeat);
        result.Booking.Should().BeNull();
    }

    [Fact]
    public void SeatValidation_ShouldRejectOccupiedSeatWithoutChangingAnySeat()
    {
        var seats = new Dictionary<string, SeatAvailabilityTypes>
        {
            ["A1"] = SeatAvailabilityTypes.AVAILABLE,
            ["A2"] = SeatAvailabilityTypes.OCCUPIED
        };

        var valid = BookingTool.AreSeatsAvailable(seats, new[] { "A1", "A2" });

        valid.Should().BeFalse();
        seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        seats["A2"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Fact]
    public void Receive_ShouldRejectNullSchedule()
    {
        Action act = () => BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), null!, Guid.NewGuid(), Guid.NewGuid(), new[] { "A1" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
    }

    private static (Schedule Schedule, Showing Showing, Guid CustomerId) CreateScheduledShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var id = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, room, id, film, DateTime.Today.AddDays(2), 12.50m);
        return (schedule, schedule.GetShowing(id)!, Guid.NewGuid());
    }
}
