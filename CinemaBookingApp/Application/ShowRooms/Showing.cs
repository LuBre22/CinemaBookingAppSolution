using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Money;

namespace CinemaBookingApp.Application.ShowRooms;

public sealed class Showing
{
    private readonly Guid _showingId;
    private readonly Film _film;
    private readonly DateTime _startingTime;
    private readonly DateTime _endingTime;
    private bool _cancelled;
    public decimal TicketPrice { get; private set; }
    public int BookedSeats { get; private set; } = 0;
    
    public Showing(Guid showingId, Film film, DateTime startingTime, Decimal ticketPrice)
    {
        if (showingId == Guid.Empty)
            throw new ArgumentException("Showing ID must not be empty.", nameof(showingId));

        ArgumentNullException.ThrowIfNull(film);

        if (!ValidateMoney.IsPositive(ticketPrice))
            throw new ArgumentOutOfRangeException(nameof(ticketPrice), "Ticket price must be positive.");

        if (!ValidateMoney.IsTwoDecimals(ticketPrice))
            throw new ArgumentOutOfRangeException(nameof(ticketPrice), "Ticket price must have at most two decimal places.");

        _showingId = showingId;
        _film = film;
        _startingTime = startingTime;
        TicketPrice = ticketPrice;
        _endingTime = startingTime.Add(film.Duration);
    }
    
    public Guid ShowingId => _showingId;
    public Film Film => _film;
    public DateTime StartingTime => _startingTime;
    public DateTime EndingTime => _endingTime;
    public FilmStatusCategories Status => GetStatus(DateTime.Now);

    internal FilmStatusCategories GetStatus(DateTime now)
    {
        if (_cancelled)
            return FilmStatusCategories.CANCELLED;
        if (now < StartingTime)
            return FilmStatusCategories.PLANNED;
        return now < EndingTime ? FilmStatusCategories.RUNNING : FilmStatusCategories.OVER;
    }

    internal void Cancel()
    {
        if (Status != FilmStatusCategories.PLANNED)
            throw new InvalidOperationException("Only planned showings can be cancelled.");

        _cancelled = true;
    }
}