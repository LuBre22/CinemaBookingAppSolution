using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Time;
using CinemaBookingApp.Domain.Payment;

namespace CinemaBookingApp.Application.Actors;

public sealed class Manager
{
    public static void CreateFilm(FilmCatalog catalog, string name, Genre genre, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var film = new Film(Guid.NewGuid(), name, genre, duration);
        catalog.AddMovie(film);
    }
    
    public static void AddShowing(FilmCatalog catalog, Schedule schedule, Room room, Guid showingId, Film film, DateTime startingTime, Decimal ticketPrice)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(film);

        if (!catalog.TryGetMovie(film.MovieId, out var catalogFilm) || catalogFilm is null)
            throw new ArgumentException("The film does not exist in the catalog.", nameof(film));

        if (!StartingTime.isFutureTime(startingTime))
            throw new InvalidOperationException("The showing must start in the future.");

        var showing = new Showing(showingId, catalogFilm, startingTime, ticketPrice, room);
        schedule.EnsureShowingIdAvailable(showingId);
        room.AddShowing(showing);
        schedule.AddShowing(room, showing);
    }

    public static void CancelShowing(Schedule schedule, Guid showingId, ExternalPaymentSystem paymentSystem)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(paymentSystem);
        var scheduleLine = schedule.ScheduleEntries.FirstOrDefault(entry => entry.Showing.ShowingId == showingId)
            ?? throw new InvalidOperationException("The showing does not exist in any schedule line.");

        lock (scheduleLine.Showing.SyncRoot)
        {
            if (scheduleLine.Showing.Status == FilmStatusCategories.PLANNED)
                scheduleLine.Showing.Cancel();
            else if (scheduleLine.Showing.Status != FilmStatusCategories.CANCELLED)
                throw new InvalidOperationException("Only planned showings can be cancelled.");
            foreach (var booking in scheduleLine.Showing.Bookings)
                if (booking.Status is BookingStatusTypes.RECEIVED or BookingStatusTypes.CONFIRMED)
                    BookingTool.CancelAndRefund(booking, paymentSystem);
            schedule.RemoveScheduleEntry(scheduleLine);
        }
    }
}
