namespace CinemaBookingApp.Application.ShowRooms;

public class Room
{
    private readonly Guid _roomNumber;
    private readonly List<Showing> _showings;

    public Room(Guid roomNumber, IEnumerable<Showing> showings)
    {
        if (roomNumber == Guid.Empty)
            throw new ArgumentException("Room number must not be empty.", nameof(roomNumber));

        ArgumentNullException.ThrowIfNull(showings);
        var showingList = showings.ToList();
        if (showingList.Exists(showing => showing is null))
            throw new ArgumentException("Showings must not contain null.", nameof(showings));

        _roomNumber = roomNumber;
        _showings = showingList;
    }
    
    public  Guid RoomNumber => _roomNumber;
    public IReadOnlyList<Showing> Showings => _showings;
}