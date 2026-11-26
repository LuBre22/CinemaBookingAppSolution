namespace CinemaBookingApp.Domain.Time;

public static class StartingTime
{
    public static bool IsValid(DateTime endPreviousShowing, DateTime startProposedShowing)
    {
        TimeSpan bufferTime = TimeSpan.FromMinutes(30);
        
        if (startProposedShowing < DateTime.Now)
            return false;
        
        var earliestStart = endPreviousShowing;
        TimeShift.Add(ref earliestStart, bufferTime);
        
        return startProposedShowing >= earliestStart;
    }
}