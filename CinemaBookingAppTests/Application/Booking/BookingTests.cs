using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Booking;

public class BookingTests
{
    [Theory]
    [InlineData(BookingStatusTypes.RECEIVED)]
    [InlineData(BookingStatusTypes.CONFIRMED)]
    [InlineData(BookingStatusTypes.DENIED)]
    [InlineData(BookingStatusTypes.CANCELLED)]
    public void Constructor_ShouldStoreDetailsAndCalculatePrice(BookingStatusTypes status)
    {
        var showing = CreateShowing(12.50m);
        var customerId = Guid.NewGuid();

        var booking = new CinemaBookingApp.Application.Booking.Booking(
            showing, customerId, new[] { "A1", "A2" }, status);

        booking.ShowingId.Should().Be(showing.ShowingId);
        booking.CustomerId.Should().Be(customerId);
        booking.SeatNumbers.Should().Equal("A1", "A2");
        booking.Price.Should().Be(25.00m);
        booking.Status.Should().Be(status);
    }

    [Fact]
    public void Constructor_ShouldSnapshotAndProtectSeatNumbers()
    {
        var seats = new List<string> { "A1", "A2" };
        var booking = new CinemaBookingApp.Application.Booking.Booking(
            CreateShowing(12.50m), Guid.NewGuid(), seats, BookingStatusTypes.RECEIVED);
        seats.Add("A3");

        booking.SeatNumbers.Should().Equal("A1", "A2");
        booking.Price.Should().Be(25.00m);
        Action mutate = () => ((ICollection<string>)booking.SeatNumbers).Add("A4");
        mutate.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Constructor_ShouldAllowEmptySeatsForLaterValidation()
    {
        var booking = new CinemaBookingApp.Application.Booking.Booking(
            CreateShowing(10m), Guid.NewGuid(), Array.Empty<string>(), BookingStatusTypes.DENIED);

        booking.SeatNumbers.Should().BeEmpty();
        booking.Price.Should().Be(0m);
    }

    [Fact]
    public void Constructor_ShouldRejectNullShowingAndSeatCollection()
    {
        var showing = CreateShowing(10m);
        Action nullShowing = () => new CinemaBookingApp.Application.Booking.Booking(
            null!, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.RECEIVED);
        Action nullSeats = () => new CinemaBookingApp.Application.Booking.Booking(
            showing, Guid.NewGuid(), null!, BookingStatusTypes.RECEIVED);

        nullShowing.Should().Throw<ArgumentNullException>().WithParameterName("showing");
        nullSeats.Should().Throw<ArgumentNullException>().WithParameterName("seatNumbers");
    }

    [Fact]
    public void Constructor_ShouldRejectPriceOverflow()
    {
        var showing = CreateShowing(decimal.MaxValue);
        Action act = () => new CinemaBookingApp.Application.Booking.Booking(
            showing, Guid.NewGuid(), new[] { "A1", "A2" }, BookingStatusTypes.RECEIVED);

        act.Should().Throw<OverflowException>();
    }

    private static Showing CreateShowing(decimal ticketPrice)
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        return new Showing(Guid.NewGuid(), film, DateTime.Today.AddDays(2), ticketPrice, room);
    }
}
