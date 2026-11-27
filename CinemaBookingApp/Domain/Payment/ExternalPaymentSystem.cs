using CinemaBookingApp.Application.Booking;

namespace CinemaBookingApp.Domain.Payment;

public class ExternalPaymentSystem
{
    public ExternalPaymentSystem(bool approve)
    {
        Approve = approve;
    }

    public bool Approve { get; }

    public bool ApprovePayment(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        return true;
    }

    public bool RejectPayment(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        return false;
    }

    public virtual bool Refund(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        return true;
    }
}