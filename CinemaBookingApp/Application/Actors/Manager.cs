using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Time;

namespace CinemaBookingApp.Application.Actors;

public sealed class Manager
{
    public void CreateFilm(FilmCatalog catalog, string name, Genre genre, TimeSpan duration)
    {
        var film = new Film(Guid.NewGuid(), name, genre, duration);
        catalog.AddMovie(film);
    }
    
    public void AddShowing(Schedule schedule, Room room, Guid showingId, Film film, DateTime startingTime, Decimal ticketPrice)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(film);
        
        if (!_isValidShowtime(startingTime, film.Duration, room))
            throw new InvalidOperationException("The showing overlaps with an existing showing in the room.");
        
        var showing = new Showing(showingId, film, startingTime, ticketPrice);
        
        var scheduleLine = schedule.GetScheduleLine(room.RoomNumber, DateOnly.FromDateTime(startingTime));
        if (scheduleLine == null)
        {
            scheduleLine = new ScheduleLine(room, DateOnly.FromDateTime(startingTime));
            schedule.AddScheduleEntry(scheduleLine);
        }
        scheduleLine.AddShowing(showing);  
    }
    
    public void CancelShowing(Schedule schedule, Guid showingId)
    {
        var scheduleLine = schedule.ScheduleEntries.FirstOrDefault(entry => entry.GetShowing(showingId) != null);
        if (scheduleLine == null)
            throw new InvalidOperationException("The showing does not exist in any schedule line.");
        
        var entry = scheduleLine.GetShowing(showingId);
        if (entry == null)
            throw new InvalidOperationException("The showing does not exist.");
        if (entry.StartingTime < DateTime.Now)
            throw new InvalidOperationException("Cannot cancel a showing that has already started.");
        
        scheduleLine.Room.RemoveShowing(showingId);
    }
    
    private bool _isValidShowtime(DateTime proposedStart, TimeSpan duration, Room room)
    {
        if (!StartingTime.isFutureTime(proposedStart))
            return false;
        
        var proposedEnd = proposedStart;
        TimeShift.Add(ref proposedEnd, duration);

        foreach (var existing in room.Showings!)
        {
            bool fitsAfter = StartingTime.IsValidBuffer(existing.EndingTime, proposedStart);
            bool fitsBefore = StartingTime.IsValidBuffer(proposedEnd, existing.StartingTime);

            if (!fitsBefore && !fitsAfter)
                return false;
        }

        return true;
    }
}