using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;

namespace CinemaBookingApp.Application.Booking;

public static class BookingTool
{
    public static BookingResult Receive(Schedule schedule, Guid showingId, Guid customerId, IEnumerable<string>? seatNumbers)
    {
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