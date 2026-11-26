namespace CinemaBookingApp.Application.Scheduler;

public sealed class Schedule
{
    public List<ScheduleEntry> ScheduleEntries { get; } = new List<ScheduleEntry>();
    
    public void AddScheduleEntry(ScheduleEntry scheduleEntry)
    {
        ArgumentNullException.ThrowIfNull(scheduleEntry);
        ScheduleEntries.Add(scheduleEntry);
    }
    
    public void RemoveScheduleEntry(ScheduleEntry scheduleEntry)
    {
        ArgumentNullException.ThrowIfNull(scheduleEntry);
        ScheduleEntries.Remove(scheduleEntry);
    }
}