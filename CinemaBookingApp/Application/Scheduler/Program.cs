using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Time;

namespace CinemaBookingApp.Application.Scheduler;

public static class Program
{
    public sealed record ProgramLine(
        Guid ShowingId,
        Guid FilmId,
        string Title,
        DateTime StartTime,
        TimeSpan Duration,
        Room Room,
        int AvailableSeats
    );
    
    public static List<ProgramLine> GetProgram(
        Schedule schedule,
        DateTime startingDate,
        DateTime endingDate
    )
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return schedule.ScheduleEntries
            .SelectMany(line => line.GetShowingsWithRooms())
            .Where(item => item.Showing.StartingTime >= startingDate
                && item.Showing.StartingTime <= endingDate
                && StartingTime.isFutureTime(item.Showing.StartingTime))
            .GroupBy(item => item.Showing.Film.MovieId)
            .SelectMany(group => group.OrderBy(item => item.Showing.StartingTime))
            .Select(item => new ProgramLine(
                item.Showing.ShowingId,
                item.Showing.Film.MovieId,
                item.Showing.Film.Title,
                item.Showing.StartingTime,
                item.Showing.Film.Duration,
                item.Room,
                item.Room.Capacity - item.Showing.BookedSeats))
            .ToList();
    }
}