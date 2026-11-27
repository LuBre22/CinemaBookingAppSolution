using CinemaBookingApp.Application.Booking;

namespace CinemaBookingApp.Domain.Payment;

public sealed class ExternalPaymentSystem
{
    public ExternalPaymentSystem(bool approve)
    {
        Approve = approve;
    }

    public bool Approve { get; }

    public void ApprovePayment(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        booking.ApplyPaymentDecision(true);
    }

    public void RejectPayment(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        booking.ApplyPaymentDecision(false);
    }
}