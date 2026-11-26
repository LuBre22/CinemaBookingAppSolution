using CinemaBookingApp.Application.ShowRooms;

namespace CinemaBookingApp.Application.Scheduler;

public sealed class ScheduleLine
{
    private readonly IReadOnlyList<Room> _rooms;
    public DateOnly Date { get; }

    internal ScheduleLine(DateOnly date, IReadOnlyList<Room> rooms)
    {
        Date = date;
        _rooms = rooms;
    }

    public IReadOnlyList<Showing> GetAllShowings()
    {
        return _rooms.SelectMany(room => room.Showings)
            .Where(IsOnDate)
            .OrderBy(showing => showing.StartingTime)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<Showing> GetShowingsForRoom(Guid roomNumber)
    {
        return _rooms.Where(room => room.RoomNumber == roomNumber)
            .SelectMany(room => room.Showings)
            .Where(IsOnDate)
            .OrderBy(showing => showing.StartingTime)
            .ToList()
            .AsReadOnly();
    }

    public Showing? GetShowing(Guid showingId)
    {
        return GetAllShowings().FirstOrDefault(showing => showing.ShowingId == showingId);
    }

    internal Room? FindRoom(Guid showingId)
    {
        return _rooms.FirstOrDefault(room =>
            room.Showings.Any(showing => showing.ShowingId == showingId && IsOnDate(showing)));
    }

    private bool IsOnDate(Showing showing)
    {
        return DateOnly.FromDateTime(showing.StartingTime) == Date;
    }
}
