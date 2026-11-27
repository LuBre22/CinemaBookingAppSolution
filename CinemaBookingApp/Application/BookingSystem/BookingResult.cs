namespace CinemaBookingApp.Application.Booking;

public sealed class BookingResult
{
    private BookingResult(BookingStatusTypes status, Booking? booking, BookingDenialReason? denialReason)
    {
        _status = status;
        Booking = booking;
        DenialReason = denialReason;
    }

    private readonly BookingStatusTypes _status;
    public BookingStatusTypes Status => Booking?.Status ?? _status;
    public Booking? Booking { get; }
    public BookingDenialReason? DenialReason { get; }

    internal static BookingResult Received(Booking booking) =>
        new(BookingStatusTypes.RECEIVED, booking, null);

    internal static BookingResult Denied(BookingDenialReason reason) =>
        new(BookingStatusTypes.DENIED, null, reason);
}
