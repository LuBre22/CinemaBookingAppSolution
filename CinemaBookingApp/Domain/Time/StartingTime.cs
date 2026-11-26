namespace CinemaBookingApp.Domain.Time;

public static class StartingTime
{
    public static bool isFutureTime(DateTime proposedStart)
    {
        return proposedStart > DateTime.Now;
    }
    
    public static bool IsValidBuffer(DateTime endPreviousShowing, DateTime startProposedShowing)
    {
        TimeSpan bufferTime = TimeSpan.FromMinutes(30);
        
        var earliestStart = endPreviousShowing;
        TimeShift.Add(ref earliestStart, bufferTime);
        
        return startProposedShowing >= earliestStart;
    }
}