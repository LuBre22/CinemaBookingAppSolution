using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FluentAssertions;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingAppAcceptanceTests.StepDefinitions;

internal sealed class CinemaFixture
{
    public readonly Schedule Schedule = new();
    public readonly FilmCatalog Catalog = new();
    public readonly ExternalAuthenticator Authenticator = new(true);
    public readonly ExternalPaymentSystem Payment = new(true);
    public readonly Bookings Bookings;
    public readonly Film Film = new(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(90));
    public readonly Guid CustomerId = Guid.NewGuid();

    public CinemaFixture()
    {
        Bookings = new Bookings(Authenticator);
        Catalog.AddMovie(Film);
    }

    public static Room NewRoom() => Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2", "A3" });

    public Showing ScheduleFuture(Room room, DateTime start, Film? film = null, decimal price = 12.50m)
    {
        var id = Guid.NewGuid();
        Manager.AddShowing(Catalog, Schedule, room, id, film ?? Film, start, price);
        return Schedule.GetShowing(id)!;
    }

    public Showing SeedHistorical(Room room, DateTime start)
    {
        var showing = new Showing(Guid.NewGuid(), Film, start, 12.50m, room);
        room.AddShowing(showing);
        Schedule.AddShowing(room, showing);
        return showing;
    }

    public CinemaBooking Book(Showing showing, Guid customerId, params string[] seats)
    {
        var result = BookingTool.Receive(Bookings, Schedule, showing.ShowingId, customerId, seats);
        result.Status.Should().Be(BookingStatusTypes.RECEIVED);
        var booking = result.Booking!;
        BookingTool.CommitBooking(Schedule, booking, Payment);
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        return booking;
    }
}
