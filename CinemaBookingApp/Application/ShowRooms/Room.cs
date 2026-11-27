using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CinemaBookingApp.Domain.Time;

namespace CinemaBookingApp.Application.ShowRooms;

public sealed class Room
{
    private static readonly ConcurrentDictionary<Guid, Lazy<Room>> Rooms = new();
    private readonly List<Showing> _showings = new();
    private readonly HashSet<Showing> _scheduledShowings = new();
    private readonly ReadOnlyCollection<Showing> _readOnlyShowings;
    private readonly object _seatLock = new();
    private ReadOnlyCollection<string> _seatIds;

    private Room(Guid roomNumber, string[] seatIds)
    {
        RoomNumber = roomNumber;
        _seatIds = Array.AsReadOnly(seatIds);
        _readOnlyShowings = _showings.AsReadOnly();
    }

    public static Room GetOrCreate(Guid roomNumber)
    {
        if (roomNumber == Guid.Empty)
            throw new ArgumentException("Room number must not be empty.", nameof(roomNumber));

        if (!Rooms.TryGetValue(roomNumber, out var room))
            throw new KeyNotFoundException($"Room {roomNumber} does not exist. Seat IDs are required to create it.");

        return room.Value;
    }

    public static Room GetOrCreate(Guid roomNumber, IEnumerable<string> seatIds)
    {
        if (roomNumber == Guid.Empty)
            throw new ArgumentException("Room number must not be empty.", nameof(roomNumber));
        ArgumentNullException.ThrowIfNull(seatIds);
        var ids = seatIds.ToArray();
        if (ids.Length == 0 || ids.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one non-empty seat ID is required.", nameof(seatIds));
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("Seat IDs must be unique.", nameof(seatIds));

        var room = Rooms.GetOrAdd(roomNumber, id => new Lazy<Room>(() => new Room(id, ids))).Value;
        lock (room._seatLock)
        {
            if (!room._seatIds.ToHashSet(StringComparer.Ordinal).SetEquals(ids))
            {
                if (room._showings.Count > 0)
                    throw new InvalidOperationException("The seat layout cannot change while the room has showings.");
                room._seatIds = Array.AsReadOnly(ids);
            }
        }
        return room;
    }

    public Guid RoomNumber { get; }
    public int Capacity => _seatIds.Count;
    public IReadOnlyList<string> SeatIds => _seatIds;
    public IReadOnlyList<Showing> Showings => _readOnlyShowings;

    internal void AddShowing(Showing showing)
    {
        ArgumentNullException.ThrowIfNull(showing);
        lock (_seatLock)
        {
            if (!ReferenceEquals(showing.Room, this) || !showing.Seats.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(_seatIds))
                throw new ArgumentException("The showing must use this room's current seat layout.", nameof(showing));
            if (showing.Status == Domain.Films.FilmStatusCategories.CANCELLED)
                throw new InvalidOperationException("A cancelled showing cannot be added to a room.");
            if (!FitsWithExistingShowings(showing))
                throw new InvalidOperationException("The showing overlaps with an existing showing in the room.");

            _showings.Add(showing);
        }
    }

    internal void RemoveShowing(Guid showingId)
    {
        var showing = _showings.FirstOrDefault(s => s.ShowingId == showingId);
        if (showing != null)
            RemoveShowing(showing);
    }

    internal void RemoveShowing(Showing showing)
    {
        lock (_seatLock)
        {
            if (_scheduledShowings.Contains(showing) && showing.Status != Domain.Films.FilmStatusCategories.CANCELLED)
                throw new InvalidOperationException("A scheduled showing must be cancelled before removal.");
            _showings.Remove(showing);
            _scheduledShowings.Remove(showing);
        }
    }

    internal void RegisterScheduledShowing(Showing showing)
    {
        if (!_showings.Contains(showing))
            throw new ArgumentException("The showing must belong to the room.", nameof(showing));
        if (!_scheduledShowings.Add(showing))
            throw new InvalidOperationException("The showing is already scheduled.");
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
