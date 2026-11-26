namespace CinemaBookingApp.Domain.Time;

public static class TimeShift
{
    public static void AddMinutes(ref DateTime time, int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutes, nameof(minutes));
        
        time = time.AddMinutes(minutes);
    }
}