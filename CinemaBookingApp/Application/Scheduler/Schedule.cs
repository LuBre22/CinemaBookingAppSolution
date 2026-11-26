using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class Schedule
{
    public List<ScheduleLine> ScheduleEntries { get; } = new List<ScheduleLine>();
    
    public void AddScheduleEntry(ScheduleLine scheduleLine)
    {
        ArgumentNullException.ThrowIfNull(scheduleLine);
        ScheduleEntries.Add(scheduleLine);
    }
    
    public void RemoveScheduleEntry(ScheduleLine scheduleLine)
    {
        ArgumentNullException.ThrowIfNull(scheduleLine);
        ScheduleEntries.Remove(scheduleLine);
    }
    
    public ScheduleLine? GetScheduleLine(Guid roomNumber, DateOnly date)
    {
        return ScheduleEntries.FirstOrDefault(entry => entry.Room.RoomNumber == roomNumber && entry.Date == date);
    }
    
    public Showing? GetShowing(Guid showingId)
    {
        foreach (var scheduleLine in ScheduleEntries)
        {
            var showing = scheduleLine.GetShowing(showingId);
            if (showing != null)
                return showing;
        }

        return null;
    }
}