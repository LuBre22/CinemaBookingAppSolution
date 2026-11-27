using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;

namespace CinemaBookingApp.Application.Booking;

public static class BookingTool
{
    public static void CommitBooking(Schedule schedule, Booking booking)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(booking);

        lock (booking.SyncRoot)
        {
            if (booking.Status != BookingStatusTypes.CONFIRMED)
                throw new InvalidOperationException("Only confirmed bookings can be committed.");
            if (booking.IsCommitted)
                throw new InvalidOperationException("The booking has already been committed.");

            if (!ReferenceEquals(schedule.GetShowing(booking.ShowingId), booking.Showing)
                || !booking.Showing.TryReserveSeats(booking.SeatNumbers))
            {
                booking.DenyReservation();
                throw new InvalidOperationException("The showing or selected seats are no longer available.");
            }

            booking.MarkCommitted();
        }
    }

    public static void CancelBooking(Booking booking, Guid customerId, ExternalAuthenticator authenticator)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(authenticator);

        lock (booking.SyncRoot)
        {
            if (customerId != booking.CustomerId || !authenticator.Authenticate(customerId))
                throw new UnauthorizedAccessException("The customer is not authorized to cancel this booking.");
            if (booking.Status is BookingStatusTypes.DENIED or BookingStatusTypes.CANCELLED)
                throw new InvalidOperationException("Denied or cancelled bookings cannot be cancelled.");

            booking.Showing.ReleaseSeatsBeforeStart(booking.IsCommitted ? booking.SeatNumbers : Array.Empty<string>());
            booking.DenyReservation();
        }
    }

    public static void VerifyPayment(Booking booking, ExternalPaymentSystem paymentSystem)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(paymentSystem);

        if (paymentSystem.Approve)
            paymentSystem.ApprovePayment(booking);
        else
            paymentSystem.RejectPayment(booking);
    }

    public static BookingResult Receive(Bookings bookings, Schedule schedule, Guid showingId, Guid customerId, IEnumerable<string>? seatNumbers)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(schedule);

        var showing = schedule.GetShowing(showingId);
        if (showing is null)
            return BookingResult.Denied(BookingDenialReason.ShowingNotFound);
        if (showing.Status != FilmStatusCategories.PLANNED)
            return BookingResult.Denied(BookingDenialReason.ShowingNotBookable);
        if (seatNumbers is null)
            return BookingResult.Denied(BookingDenialReason.NoSeatsSelected);

        var seats = seatNumbers.ToArray();
        if (seats.Length == 0)
            return BookingResult.Denied(BookingDenialReason.NoSeatsSelected);
        if (!AreSeatsAvailable(showing.Seats, seats))
            return BookingResult.Denied(BookingDenialReason.InvalidOrUnavailableSeat);

        var booking = new Booking(showing, customerId, seats, BookingStatusTypes.RECEIVED);
        bookings.Add(booking);
        return BookingResult.Received(booking);
    }

    internal static bool AreSeatsAvailable(IReadOnlyDictionary<string, SeatAvailabilityTypes> availableSeats, IEnumerable<string> seatNumbers)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var seat in seatNumbers)
        {
            if (string.IsNullOrWhiteSpace(seat)
                || !seen.Add(seat)
                || !availableSeats.TryGetValue(seat, out var availability)
                || availability != SeatAvailabilityTypes.AVAILABLE)
                return false;
        }

        return true;
    }
}