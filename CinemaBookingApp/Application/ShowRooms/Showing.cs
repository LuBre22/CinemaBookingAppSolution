using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Money;
using System.Collections.ObjectModel;
using CinemaBookingApp.Application.Booking;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingApp.Application.ShowRooms;

public sealed class Showing
{
    private readonly Guid _showingId;
    private readonly Film _film;
    private readonly DateTime _startingTime;
    private readonly DateTime _endingTime;
    private bool _cancelled;
    internal object SyncRoot { get; } = new();
    private readonly Dictionary<string, SeatAvailabilityTypes> _seats;
    private readonly List<CinemaBooking> _bookings = new();
    public decimal TicketPrice { get; private set; }
    public Room Room { get; }
    public IReadOnlyDictionary<string, SeatAvailabilityTypes> Seats { get; }
    
    public Showing(Guid showingId, Film film, DateTime startingTime, Decimal ticketPrice, Room room)
    {
        if (showingId == Guid.Empty)
            throw new ArgumentException("Showing ID must not be empty.", nameof(showingId));

        ArgumentNullException.ThrowIfNull(film);
        ArgumentNullException.ThrowIfNull(room);

        if (!ValidateMoney.IsPositive(ticketPrice))
            throw new ArgumentOutOfRangeException(nameof(ticketPrice), "Ticket price must be positive.");

        if (!ValidateMoney.IsTwoDecimals(ticketPrice))
            throw new ArgumentOutOfRangeException(nameof(ticketPrice), "Ticket price must have at most two decimal places.");

        _showingId = showingId;
        _film = film;
        _startingTime = startingTime;
        TicketPrice = ticketPrice;
        _endingTime = startingTime.Add(film.Duration);
        Room = room;
        _seats = room.SeatIds.ToDictionary(id => id, _ => SeatAvailabilityTypes.AVAILABLE, StringComparer.Ordinal);
        Seats = new ReadOnlyDictionary<string, SeatAvailabilityTypes>(_seats);
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
        lock (SyncRoot)
        {
            if (Status != FilmStatusCategories.PLANNED)
                throw new InvalidOperationException("Only planned showings can be cancelled.");

            _cancelled = true;
        }
    }

    internal bool TryReserveSeats(IReadOnlyList<string> seatNumbers)
    {
        lock (SyncRoot)
        {
            if (Status != FilmStatusCategories.PLANNED || seatNumbers.Count == 0
                || !BookingTool.AreSeatsAvailable(_seats, seatNumbers))
                return false;

            foreach (var seat in seatNumbers)
                _seats[seat] = SeatAvailabilityTypes.OCCUPIED;

            return true;
        }
    }

    internal void ReleaseSeats(IReadOnlyList<string> seatNumbers)
    {
        lock (SyncRoot)
        {
            foreach (var seat in seatNumbers)
                _seats[seat] = SeatAvailabilityTypes.AVAILABLE;
        }
    }

    internal void RegisterBooking(CinemaBooking booking)
    {
        lock (SyncRoot)
        {
            if (Status == FilmStatusCategories.CANCELLED &&
                booking.Status is BookingStatusTypes.RECEIVED or BookingStatusTypes.CONFIRMED)
                throw new InvalidOperationException("Active bookings cannot be added to a cancelled showing.");
            if (!_bookings.Contains(booking))
                _bookings.Add(booking);
        }
    }

    internal IReadOnlyList<CinemaBooking> Bookings => _bookings;
}