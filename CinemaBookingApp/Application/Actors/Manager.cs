using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Time;

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

        var showing = new Showing(showingId, catalogFilm, startingTime, ticketPrice);
        room.AddShowing(showing);
        schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(startingTime)).AddShowing(room, showing);
    }

    public static void CancelShowing(Schedule schedule, Guid showingId)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var scheduleLine = schedule.ScheduleEntries.FirstOrDefault(entry => entry.FindShowing(showingId) != null)
            ?? throw new InvalidOperationException("The showing does not exist in any schedule line.");

        var (room, showing) = scheduleLine.FindShowing(showingId)!.Value;
        if (!StartingTime.isFutureTime(showing.StartingTime))
            throw new InvalidOperationException("Cannot cancel a showing that has already started.");

        room.RemoveShowing(showing);
        scheduleLine.RemoveShowing(showing);
    }
}
