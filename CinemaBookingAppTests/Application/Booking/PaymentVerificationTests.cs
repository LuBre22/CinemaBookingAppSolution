using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FluentAssertions;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingAppTests.Application.Booking;

public class PaymentVerificationTests
{
    [Fact]
    public void PaymentStub_ReturnsDecisionsWithoutChangingBooking()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var showing = new Showing(Guid.NewGuid(), film, DateTime.Now.AddDays(1), 12m, room);
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.RECEIVED);
        var payment = new ExternalPaymentSystem(true);

        payment.ApprovePayment(booking).Should().BeTrue();
        payment.RejectPayment(booking).Should().BeFalse();
        payment.Refund(booking).Should().BeTrue();
        booking.Status.Should().Be(BookingStatusTypes.RECEIVED);
    }

    [Fact]
    public void PaymentStub_RejectsNullBookings()
    {
        var payment = new ExternalPaymentSystem(true);
        Action approve = () => payment.ApprovePayment(null!);
        Action reject = () => payment.RejectPayment(null!);
        Action refund = () => payment.Refund(null!);
        approve.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        reject.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        refund.Should().Throw<ArgumentNullException>().WithParameterName("booking");
    }
}
