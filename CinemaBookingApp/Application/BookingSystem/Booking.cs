using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Booking;

public sealed class Booking
{
    public Booking(Showing showing, Guid customerId, IEnumerable<string> seatNumbers, BookingStatusTypes status)
    {
        ArgumentNullException.ThrowIfNull(showing);
        ArgumentNullException.ThrowIfNull(seatNumbers);

        var seats = seatNumbers.ToArray();
        Showing = showing;
        ShowingId = showing.ShowingId;
        CustomerId = customerId;
        SeatNumbers = Array.AsReadOnly(seats);
        Price = showing.TicketPrice * seats.Length;
        Status = status;
    }

    public Guid ShowingId { get; }
    public Guid CustomerId { get; }
    public IReadOnlyList<string> SeatNumbers { get; }
    public decimal Price { get; }
    public BookingStatusTypes Status { get; private set; }
    internal Showing Showing { get; }
    internal object SyncRoot { get; } = new();
    internal bool IsCommitted { get; private set; }

    internal void MarkCommitted()
    {
        IsCommitted = true;
        Status = BookingStatusTypes.CONFIRMED;
    }

    internal void DenyReservation()
    {
        Status = BookingStatusTypes.DENIED;
    }

    internal void MarkCancelled()
    {
        IsCommitted = false;
        Status = BookingStatusTypes.CANCELLED;
    }
}