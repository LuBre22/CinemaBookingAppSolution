using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FluentAssertions;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingAppTests.Application.Booking;

public class PaymentVerificationTests
{
    [Theory]
    [InlineData(true, BookingStatusTypes.CONFIRMED)]
    [InlineData(false, BookingStatusTypes.DENIED)]
    public void VerifyPayment_ShouldApplyConfiguredOutcomeWithoutReservingSeats(bool approve, BookingStatusTypes expected)
    {
        var (result, showing) = ReceiveBooking();
        var booking = result.Booking!;

        BookingTool.VerifyPayment(booking, new ExternalPaymentSystem(approve));

        booking.Status.Should().Be(expected);
        result.Status.Should().Be(expected);
        result.Booking.Should().BeSameAs(booking);
        booking.Price.Should().Be(25m);
        booking.SeatNumbers.Should().Equal("A1", "A2");
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Theory]
    [InlineData(BookingStatusTypes.CONFIRMED)]
    [InlineData(BookingStatusTypes.DENIED)]
    [InlineData(BookingStatusTypes.CANCELLED)]
    public void VerifyPayment_ShouldRejectNonReceivedBooking(BookingStatusTypes status)
    {
        var (_, showing) = ReceiveBooking();
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, status);

        Action act = () => BookingTool.VerifyPayment(booking, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(status);
    }

    [Fact]
    public void VerifyPayment_ShouldRejectRepeatedVerification()
    {
        var (result, _) = ReceiveBooking();
        var booking = result.Booking!;
        BookingTool.VerifyPayment(booking, new ExternalPaymentSystem(true));

        Action act = () => BookingTool.VerifyPayment(booking, new ExternalPaymentSystem(false));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
    }

    [Fact]
    public void VerifyPayment_ShouldRejectNullArguments()
    {
        var (result, _) = ReceiveBooking();
        Action nullBooking = () => BookingTool.VerifyPayment(null!, new ExternalPaymentSystem(true));
        Action nullPayment = () => BookingTool.VerifyPayment(result.Booking!, null!);

        nullBooking.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        nullPayment.Should().Throw<ArgumentNullException>().WithParameterName("paymentSystem");
        result.Booking!.Status.Should().Be(BookingStatusTypes.RECEIVED);
    }

    [Theory]
    [InlineData(true, BookingStatusTypes.CONFIRMED)]
    [InlineData(false, BookingStatusTypes.DENIED)]
    public void MockPaymentMethods_ShouldUpdateReceivedBooking(bool approve, BookingStatusTypes expected)
    {
        var (result, _) = ReceiveBooking();
        var booking = result.Booking!;
        var paymentSystem = new ExternalPaymentSystem(!approve);

        if (approve)
            paymentSystem.ApprovePayment(booking);
        else
            paymentSystem.RejectPayment(booking);

        booking.Status.Should().Be(expected);
        result.Status.Should().Be(expected);
    }

    [Fact]
    public void MockPaymentMethods_ShouldRejectNullAndPreviouslyProcessedBookings()
    {
        var (result, _) = ReceiveBooking();
        var booking = result.Booking!;
        var paymentSystem = new ExternalPaymentSystem(true);
        Action nullApproval = () => paymentSystem.ApprovePayment(null!);
        Action nullRejection = () => paymentSystem.RejectPayment(null!);
        nullApproval.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        nullRejection.Should().Throw<ArgumentNullException>().WithParameterName("booking");

        paymentSystem.RejectPayment(booking);
        Action approval = () => paymentSystem.ApprovePayment(booking);
        Action rejection = () => paymentSystem.RejectPayment(booking);
        approval.Should().Throw<InvalidOperationException>();
        rejection.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.DENIED);
    }

    private static (BookingResult Result, Showing Showing) ReceiveBooking()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var id = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, room, id, film, DateTime.Today.AddDays(2), 12.50m);
        var showing = schedule.GetShowing(id)!;
        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, id, Guid.NewGuid(), new[] { "A1", "A2" });
        return (result, showing);
    }
}
