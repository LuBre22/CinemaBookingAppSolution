using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Time;

namespace CinemaBookingApp.Application.Actors;

public sealed class Manager
{
    public void CreateFilm(FilmCatalog catalog, string name, Genre genre, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var film = new Film(Guid.NewGuid(), name, genre, duration);
        catalog.AddMovie(film);
    }
    
    public void AddShowing(Schedule schedule, Room room, Guid showingId, Film film, DateTime startingTime, Decimal ticketPrice)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(film);

        if (!StartingTime.isFutureTime(startingTime))
            throw new InvalidOperationException("The showing must start in the future.");

        room.AddShowing(new Showing(showingId, film, startingTime, ticketPrice));

        schedule.RegisterRoom(room);
        schedule.GetOrCreateScheduleLine(DateOnly.FromDateTime(startingTime));
    }

    public void CancelShowing(Schedule schedule, Guid showingId)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var scheduleLine = schedule.ScheduleEntries.FirstOrDefault(entry => entry.FindRoom(showingId) != null)
            ?? throw new InvalidOperationException("The showing does not exist in any schedule line.");

        var room = scheduleLine.FindRoom(showingId)!;
        var showing = scheduleLine.GetShowing(showingId)!;
        if (!StartingTime.isFutureTime(showing.StartingTime))
            throw new InvalidOperationException("Cannot cancel a showing that has already started.");

        room.RemoveShowing(showingId);
    }
}
