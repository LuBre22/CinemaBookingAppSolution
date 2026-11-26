using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class ScheduleEntry
{
    private readonly Room _room;
    private readonly DateOnly _date;
    
    public ScheduleEntry(Room room, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(room);
        _room = room;
        _date = date;
    }
    
    public Room Room => _room;
    public DateOnly Date => _date;
    
    public IReadOnlyList<Showing> GetAllShowings()
    {
        return _room.Showings;
    }
    
    public Showing GetShowing(Guid showingId)
    {
        return _room.Showings.FirstOrDefault(showing => showing.ShowingId == showingId)
               ?? throw new ArgumentException(
                   $"Showing with ID {showingId} not found.", nameof(showingId));
    }
}