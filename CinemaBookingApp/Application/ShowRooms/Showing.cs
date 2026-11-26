using CinemaBookingApp.Domain.Films;

namespace CinemaBookingApp.Application.ShowRooms;

public class Showing
{
    private readonly Guid _showingId;
    private readonly Film _film;
    private readonly DateTime _startingTime;
    private readonly DateTime _endingTime;
    
    public Showing(Guid showingId, Film film, DateTime startingTime)
    {
        if (showingId == Guid.Empty)
            throw new ArgumentException("Showing ID must not be empty.", nameof(showingId));

        ArgumentNullException.ThrowIfNull(film);

        _showingId = showingId;
        _film = film;
        _startingTime = startingTime;
        _endingTime = startingTime.Add(film.Duration);
    }
    
    public Guid ShowingId => _showingId;
    public Film Film => _film;
    public DateTime StartingTime => _startingTime;
    public DateTime EndingTime => _endingTime;
}