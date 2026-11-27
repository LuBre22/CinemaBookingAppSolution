using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class Schedule
{
    private readonly List<ScheduleLine> _entries = new();
    private readonly List<ScheduleLine> _cancelledEntries = new();

    public IReadOnlyList<ScheduleLine> ScheduleEntries => _entries.AsReadOnly();
    public IReadOnlyList<ScheduleLine> CancelledEntries => _cancelledEntries.AsReadOnly();

    internal void AddShowing(Room room, Showing showing)
    {
        var line = new ScheduleLine(room, showing);
        EnsureShowingIdAvailable(showing.ShowingId);
        room.RegisterScheduledShowing(showing);
        _entries.Add(line);
    }

    internal void EnsureShowingIdAvailable(Guid showingId)
    {
        if (_entries.Any(line => line.Showing.ShowingId == showingId)
            || _cancelledEntries.Any(line => line.Showing.ShowingId == showingId))
            throw new ArgumentException("The showing ID already exists in this schedule.", nameof(showingId));
    }

    public void RemoveScheduleEntry(ScheduleLine scheduleLine)
    {
        ArgumentNullException.ThrowIfNull(scheduleLine);
        if (!_entries.Contains(scheduleLine))
            throw new ArgumentException("The schedule line does not belong to this schedule.", nameof(scheduleLine));
        if (scheduleLine.Showing.Status != FilmStatusCategories.CANCELLED)
            throw new InvalidOperationException("Only cancelled showings can be removed from the schedule.");

        scheduleLine.Room.RemoveShowing(scheduleLine.Showing);
        _entries.Remove(scheduleLine);
        _cancelledEntries.Add(scheduleLine);
    }

    public Showing? GetShowing(Guid showingId)
    {
        return _entries.Select(line => line.Showing)
            .FirstOrDefault(showing => showing.ShowingId == showingId);
    }
}
