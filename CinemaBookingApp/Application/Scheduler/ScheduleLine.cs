using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class ScheduleLine
{
    internal ScheduleLine(Room room, Showing showing)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(showing);
        if (!room.Showings.Contains(showing))
            throw new ArgumentException("The showing must belong to the room.", nameof(showing));

        Room = room;
        Showing = showing;
    }

    public Room Room { get; }
    public Showing Showing { get; }
    public DateOnly Date => DateOnly.FromDateTime(Showing.StartingTime);
}
