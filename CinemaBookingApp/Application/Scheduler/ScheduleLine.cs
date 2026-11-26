using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class ScheduleLine
{
    private readonly Room _room;
    private readonly DateOnly _date;
    
    public ScheduleLine(Room room, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(room);
        _room = room;
        _date = date;
    }
    
    public Room Room => _room;
    public DateOnly Date => _date;
    
    public IReadOnlyList<Showing>? GetAllShowings()
    {
        return _room.Showings;
    }
    
    public Showing? GetShowing(Guid showingId)
    {
        return _room.Showings!.FirstOrDefault(showing => showing.ShowingId == showingId);
    }
    
    public void AddShowing(Showing showing)
    {
        ArgumentNullException.ThrowIfNull(showing);
        _room.AddShowing(showing);
    }
}