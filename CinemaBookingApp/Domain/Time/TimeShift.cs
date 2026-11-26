namespace CinemaBookingApp.Domain.Time;

public static class TimeShift
{
    public static void Add(ref DateTime start, TimeSpan duration)
    {
        start = start.Add(duration);
    }
}