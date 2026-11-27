using CinemaBookingApp.Domain.Authentification;

namespace CinemaBookingApp.Application.Booking;

public sealed class Bookings
{
    private readonly ExternalAuthenticator _authenticator;
    private readonly List<Booking> _bookings = new();
    private readonly object _lock = new();

    public Bookings(ExternalAuthenticator authenticator)
    {
        ArgumentNullException.ThrowIfNull(authenticator);
        _authenticator = authenticator;
    }

    public void Add(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);
        lock (_lock)
        {
            if (_bookings.Contains(booking))
                throw new InvalidOperationException("The booking is already stored.");
            _bookings.Add(booking);
        }
    }

    public IReadOnlyList<Booking> ViewMyBookings(Guid customerId)
    {
        if (!_authenticator.Authenticate(customerId))
            throw new UnauthorizedAccessException("The customer could not be authenticated.");

        lock (_lock)
        {
            return Array.AsReadOnly(_bookings.Where(booking => booking.CustomerId == customerId).ToArray());
        }
    }
}