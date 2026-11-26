namespace CinemaBookingApp.Application.ShowRooms;

public sealed class Room
{
    private readonly Guid _roomNumber;
    public List<Showing>? Showings { get; } = new List<Showing>();

    public Room(Guid roomNumber, IEnumerable<Showing> showings)
    {
        _roomNumber = roomNumber;
        Showings?.AddRange(showings);
    }
    
    public void AddShowing(Showing showing)
    {
        Showings?.Add(showing);
    }
    
    public void RemoveShowing(Guid showingId)
    {
        var showing = Showings?.FirstOrDefault(s => s.ShowingId == showingId);
        if (showing != null)
            Showings?.Remove(showing);
    }
    
    public  Guid RoomNumber => _roomNumber;
}