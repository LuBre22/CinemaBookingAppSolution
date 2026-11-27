using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CinemaBookingApp.Domain.Time;

namespace CinemaBookingApp.Application.ShowRooms;

public sealed class Room
{
    private static readonly ConcurrentDictionary<Guid, Lazy<Room>> Rooms = new();
    private readonly List<Showing> _showings = new();
    private readonly Dictionary<Showing, object> _registrations = new();
    private readonly ReadOnlyCollection<Showing> _readOnlyShowings;

    private Room(Guid roomNumber, int capacity)
    {
        RoomNumber = roomNumber;
        Capacity = capacity;
        _readOnlyShowings = _showings.AsReadOnly();
    }

    public static Room GetOrCreate(Guid roomNumber)
    {
        if (roomNumber == Guid.Empty)
            throw new ArgumentException("Room number must not be empty.", nameof(roomNumber));

        if (!Rooms.TryGetValue(roomNumber, out var room))
            throw new KeyNotFoundException($"Room {roomNumber} does not exist. A capacity is required to create it.");

        return room.Value;
    }

    public static Room GetOrCreate(Guid roomNumber, int capacity)
    {
        if (roomNumber == Guid.Empty)
            throw new ArgumentException("Room number must not be empty.", nameof(roomNumber));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        var room = Rooms.GetOrAdd(roomNumber, id => new Lazy<Room>(() => new Room(id, capacity))).Value;
        room.Capacity = capacity;
        return room;
    }

    public Guid RoomNumber { get; }
    public int Capacity { get; private set; }
    public IReadOnlyList<Showing> Showings => _readOnlyShowings;

    public void AddShowing(Showing showing)
    {
        ArgumentNullException.ThrowIfNull(showing);
        if (!FitsWithExistingShowings(showing))
            throw new InvalidOperationException("The showing overlaps with an existing showing in the room.");

        _showings.Add(showing);
        _registrations.Add(showing, new object());
    }

    public void RemoveShowing(Guid showingId)
    {
        var showing = _showings.FirstOrDefault(s => s.ShowingId == showingId);
        if (showing != null)
            RemoveShowing(showing);
    }

    internal void RemoveShowing(Showing showing)
    {
        _showings.Remove(showing);
        _registrations.Remove(showing);
    }

    internal object? GetShowingRegistration(Showing showing)
    {
        return _registrations.GetValueOrDefault(showing);
    }

    private bool FitsWithExistingShowings(Showing proposed)
    {
        foreach (var existing in _showings)
        {
            bool fitsAfter = StartingTime.IsValidBuffer(existing.EndingTime, proposed.StartingTime);
            bool fitsBefore = StartingTime.IsValidBuffer(proposed.EndingTime, existing.StartingTime);

            if (!fitsBefore && !fitsAfter)
                return false;
        }

        return true;
    }
}
