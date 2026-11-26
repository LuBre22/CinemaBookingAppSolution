using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class Schedule
{
    private readonly List<Room> _rooms = new();
    private readonly List<ScheduleLine> _entries = new();

    public IReadOnlyList<ScheduleLine> ScheduleEntries => _entries.AsReadOnly();

    public ScheduleLine GetOrCreateScheduleLine(DateOnly date)
    {
        var existing = GetScheduleLine(date);
        if (existing != null)
            return existing;

        var line = new ScheduleLine(date, _rooms.AsReadOnly());
        _entries.Add(line);
        return line;
    }

    public void RemoveScheduleEntry(ScheduleLine scheduleLine)
    {
        ArgumentNullException.ThrowIfNull(scheduleLine);
        _entries.Remove(scheduleLine);
    }

    public ScheduleLine? GetScheduleLine(DateOnly date)
    {
        return _entries.FirstOrDefault(entry => entry.Date == date);
    }

    public Showing? GetShowing(Guid showingId)
    {
        return _entries.Select(line => line.GetShowing(showingId))
            .FirstOrDefault(showing => showing != null);
    }

    internal void RegisterRoom(Room room)
    {
        if (!_rooms.Contains(room))
            _rooms.Add(room);
    }
}
