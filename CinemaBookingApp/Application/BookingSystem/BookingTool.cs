using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;

namespace CinemaBookingApp.Application.Booking;

public static class BookingTool
{
    public static void CommitBooking(Schedule schedule, Booking booking, ExternalPaymentSystem paymentSystem)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(paymentSystem);

        lock (booking.Showing.SyncRoot)
        lock (booking.SyncRoot)
        {
            if (booking.Status != BookingStatusTypes.RECEIVED)
                throw new InvalidOperationException("Only received bookings can be committed.");

            if (!VerifyPayment(booking, paymentSystem))
            {
                booking.DenyReservation();
                return;
            }

            if (!ReferenceEquals(schedule.GetShowing(booking.ShowingId), booking.Showing)
                || !booking.Showing.TryReserveSeats(booking.SeatNumbers))
            {
                booking.DenyReservation();
                if (!paymentSystem.Refund(booking))
                    throw new InvalidOperationException("The booking could not be committed and its refund failed.");
                throw new InvalidOperationException("The showing or selected seats are no longer available.");
            }

            booking.Showing.RegisterBooking(booking);
            booking.MarkCommitted();
        }
    }

    public static void CancelBooking(Booking booking, Guid customerId, ExternalAuthenticator authenticator, ExternalPaymentSystem paymentSystem)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(authenticator);
        ArgumentNullException.ThrowIfNull(paymentSystem);

        lock (booking.Showing.SyncRoot)
        lock (booking.SyncRoot)
        {
            if (customerId != booking.CustomerId || !authenticator.Authenticate(customerId))
                throw new UnauthorizedAccessException("The customer is not authorized to cancel this booking.");
            if (booking.Status != BookingStatusTypes.CONFIRMED)
                throw new InvalidOperationException("Only confirmed bookings can be cancelled.");
            if (booking.Showing.Status != FilmStatusCategories.PLANNED)
                throw new InvalidOperationException("Only bookings for future scheduled showings can be cancelled.");

            CancelAndRefund(booking, paymentSystem);
        }
    }

    private static bool VerifyPayment(Booking booking, ExternalPaymentSystem paymentSystem)
    {
        return paymentSystem.Approve ? paymentSystem.ApprovePayment(booking) : paymentSystem.RejectPayment(booking);
    }

    internal static void CancelAndRefund(Booking booking, ExternalPaymentSystem paymentSystem)
    {
        lock (booking.SyncRoot)
        {
            if (!paymentSystem.Refund(booking))
                throw new InvalidOperationException("The booking refund failed.");
            if (booking.IsCommitted)
                booking.Showing.ReleaseSeats(booking.SeatNumbers);
            booking.MarkCancelled();
        }
    }

    public static BookingResult Receive(Bookings bookings, Schedule schedule, Guid showingId, Guid customerId, IEnumerable<string>? seatNumbers)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(schedule);

        var showing = schedule.GetShowing(showingId);
        if (showing is null)
            return BookingResult.Denied(BookingDenialReason.ShowingNotFound);
        lock (showing.SyncRoot)
        {
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