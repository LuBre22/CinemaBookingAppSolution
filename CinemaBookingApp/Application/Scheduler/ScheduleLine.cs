using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class ScheduleLine
{
    private readonly List<(Room Room, Showing Showing, object Registration)> _showings = new();
    public DateOnly Date { get; }

    internal ScheduleLine(DateOnly date)
    {
        Date = date;
    }

    internal void AddShowing(Room room, Showing showing)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(showing);
        if (DateOnly.FromDateTime(showing.StartingTime) != Date)
            throw new ArgumentException("The showing must start on the schedule line's date.", nameof(showing));
        var registration = room.GetShowingRegistration(showing);
        if (registration is null)
            throw new ArgumentException("The showing must belong to the room.", nameof(showing));
        DiscardRemovedShowings();
        if (_showings.Any(item => ReferenceEquals(item.Showing, showing)))
            throw new InvalidOperationException("The showing is already on this schedule line.");

        _showings.Add((room, showing, registration));
    }

    internal void RemoveShowing(Showing showing)
    {
        var index = _showings.FindIndex(item => ReferenceEquals(item.Showing, showing));
        if (index >= 0)
            _showings.RemoveAt(index);
    }

    public IReadOnlyList<Showing> GetAllShowings()
    {
        return GetShowingsWithRooms()
            .Select(item => item.Showing)
            .ToList()
            .AsReadOnly();
    }

    internal IEnumerable<(Room Room, Showing Showing)> GetShowingsWithRooms()
    {
        DiscardRemovedShowings();
        return _showings
            .Select(item => (item.Room, item.Showing))
            .OrderBy(item => item.Showing.StartingTime);
    }

    private void DiscardRemovedShowings()
    {
        // A re-added showing receives a new registration and must be scheduled again.
        _showings.RemoveAll(item =>
            !ReferenceEquals(item.Registration, item.Room.GetShowingRegistration(item.Showing)));
    }

    public IReadOnlyList<Showing> GetShowingsForRoom(Guid roomNumber)
    {
        return GetShowingsWithRooms()
            .Where(item => item.Room.RoomNumber == roomNumber)
            .Select(item => item.Showing)
            .ToList()
            .AsReadOnly();
    }

    public Showing? GetShowing(Guid showingId)
    {
        return GetAllShowings().FirstOrDefault(showing => showing.ShowingId == showingId);
    }

    internal (Room Room, Showing Showing)? FindShowing(Guid showingId)
    {
        foreach (var item in GetShowingsWithRooms())
            if (item.Showing.ShowingId == showingId)
                return item;
        return null;
    }
}
