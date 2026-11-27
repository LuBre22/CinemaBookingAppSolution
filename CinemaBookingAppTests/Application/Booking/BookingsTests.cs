using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FluentAssertions;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingAppTests.Application.Booking;

public class BookingsTests
{
    [Fact]
    public void ViewMyBookings_ReturnsAllCreatedBookingsForCustomerAcrossStatusChanges()
    {
        var (schedule, showing) = CreateShowing();
        var bookings = new Bookings(new ExternalAuthenticator(true));
        var customer = Guid.NewGuid();
        var received = BookingTool.Receive(bookings, schedule, showing.ShowingId, customer, new[] { "A1" }).Booking!;
        var confirmed = BookingTool.Receive(bookings, schedule, showing.ShowingId, customer, new[] { "A2" }).Booking!;
        var denied = BookingTool.Receive(bookings, schedule, showing.ShowingId, customer, new[] { "A3" }).Booking!;
        var cancelled = new CinemaBooking(showing, customer, new[] { "A1" }, BookingStatusTypes.CANCELLED);
        bookings.Add(cancelled);
        var other = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        BookingTool.CommitBooking(schedule, confirmed, new ExternalPaymentSystem(true));
        BookingTool.CommitBooking(schedule, denied, new ExternalPaymentSystem(false));

        var result = bookings.ViewMyBookings(customer);

        result.Should().Equal(received, confirmed, denied, cancelled);
        result.Should().NotContain(other);
        result.Select(booking => booking.Status).Should().Equal(BookingStatusTypes.RECEIVED,
            BookingStatusTypes.CONFIRMED, BookingStatusTypes.DENIED, BookingStatusTypes.CANCELLED);
        BookingTool.CancelBooking(confirmed, customer, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));
        bookings.ViewMyBookings(customer).Should().ContainSingle(b => ReferenceEquals(b, confirmed) && b.Status == BookingStatusTypes.CANCELLED);
    }

    [Fact]
    public void ViewMyBookings_ReturnsAnEmptySnapshotAndDoesNotExposeStorage()
    {
        var (schedule, showing) = CreateShowing();
        var customer = Guid.NewGuid();
        var bookings = new Bookings(new ExternalAuthenticator(true));
        var snapshot = bookings.ViewMyBookings(customer);
        var booking = BookingTool.Receive(bookings, schedule, showing.ShowingId, customer, new[] { "A1" }).Booking!;

        snapshot.Should().BeEmpty();
        var populated = bookings.ViewMyBookings(customer);
        populated.Should().Equal(booking);
        if (populated is IList<CinemaBooking> list)
        {
            Action clear = () => list.Clear();
            clear.Should().Throw<NotSupportedException>();
        }
        bookings.ViewMyBookings(customer).Should().Equal(booking);
    }

    [Fact]
    public void ViewMyBookings_RejectsDeniedAuthenticationEvenForUnknownCustomer()
    {
        var bookings = new Bookings(new ExternalAuthenticator(false));
        var (_, showing) = CreateShowing();
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.RECEIVED);
        bookings.Add(booking);
        Action existingCustomer = () => bookings.ViewMyBookings(booking.CustomerId);
        Action unknownCustomer = () => bookings.ViewMyBookings(Guid.NewGuid());
        existingCustomer.Should().Throw<UnauthorizedAccessException>();
        unknownCustomer.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Receive_DoesNotStoreRejectedRequests()
    {
        var (schedule, showing) = CreateShowing();
        var bookings = new Bookings(new ExternalAuthenticator(true));
        var customer = Guid.NewGuid();

        var result = BookingTool.Receive(bookings, schedule, showing.ShowingId, customer, Array.Empty<string>());

        result.Status.Should().Be(BookingStatusTypes.DENIED);
        result.Booking.Should().BeNull();
        bookings.ViewMyBookings(customer).Should().BeEmpty();
    }

    [Fact]
    public void Bookings_RejectNullInputsAndDuplicateInstances()
    {
        var (_, showing) = CreateShowing();
        var bookings = new Bookings(new ExternalAuthenticator(true));
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.RECEIVED);
        Action nullAuthenticator = () => new Bookings(null!);
        Action nullBooking = () => bookings.Add(null!);
        Action nullHolder = () => BookingTool.Receive(null!, new Schedule(), Guid.NewGuid(), Guid.NewGuid(), new[] { "A1" });
        bookings.Add(booking);
        Action duplicate = () => bookings.Add(booking);

        nullAuthenticator.Should().Throw<ArgumentNullException>().WithParameterName("authenticator");
        nullBooking.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        nullHolder.Should().Throw<ArgumentNullException>().WithParameterName("bookings");
        duplicate.Should().Throw<InvalidOperationException>();
        bookings.ViewMyBookings(booking.CustomerId).Should().Equal(booking);
    }

    private static (Schedule Schedule, Showing Showing) CreateShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2", "A3" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var id = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, room, id, film, DateTime.Today.AddDays(2), 12.50m);
        return (schedule, schedule.GetShowing(id)!);
    }
}
