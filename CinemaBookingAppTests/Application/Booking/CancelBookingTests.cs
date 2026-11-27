using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FluentAssertions;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;
using CinemaProgram = CinemaBookingApp.Application.Scheduler.Program;

namespace CinemaBookingAppTests.Application.Booking;

public class CancelBookingTests
{
    [Fact]
    public void CancelBooking_ReleasesOnlyItsSeatsAndMakesThemBookableAgain()
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var result = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1", "A2" });
        var booking = result.Booking!;
        BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));
        var other = ReceiveAndCommit(schedule, showing, "A3");

        BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));

        booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
        result.Status.Should().Be(BookingStatusTypes.CANCELLED);
        other.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        showing.Seats["A3"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        CinemaProgram.GetProgram(schedule, showing.StartingTime.AddMinutes(-1), showing.StartingTime.AddHours(2))
            .Single().AvailableSeats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1", "A2" }).Status
            .Should().Be(BookingStatusTypes.RECEIVED);
    }

    [Fact]
    public void CancelBooking_UncommittedConfirmedBookingDoesNotReleaseAnotherBookingsSeat()
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var pending = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.CONFIRMED);
        var owner = ReceiveAndCommit(schedule, showing, "A1");

        BookingTool.CancelBooking(pending, pending.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));

        pending.Status.Should().Be(BookingStatusTypes.CANCELLED);
        owner.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void CancelBooking_RejectsWrongCustomerOrRejectedAuthentication(bool matches, bool approved)
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var booking = ReceiveAndCommit(schedule, showing, "A1");
        var customer = matches ? booking.CustomerId : Guid.NewGuid();

        Action act = () => BookingTool.CancelBooking(booking, customer, new ExternalAuthenticator(approved), new ExternalPaymentSystem(true));

        act.Should().Throw<UnauthorizedAccessException>();
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Theory]
    [InlineData(BookingStatusTypes.DENIED)]
    [InlineData(BookingStatusTypes.CANCELLED)]
    [InlineData(BookingStatusTypes.RECEIVED)]
    public void CancelBooking_RejectsAlreadyDeniedOrCancelled(BookingStatusTypes status)
    {
        var (_, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, status);
        Action act = () => BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(status);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-120)]
    public void CancelBooking_RejectsStartedOrFinishedShowing(int minutes)
    {
        var (_, showing) = CreateShowing(DateTime.Now.AddMinutes(minutes));
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.CONFIRMED);
        Action act = () => BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void CancelBooking_RejectsRepeatedCancellationWithoutReleasingARebookedSeat()
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var first = ReceiveAndCommit(schedule, showing, "A1");
        BookingTool.CancelBooking(first, first.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));
        var second = ReceiveAndCommit(schedule, showing, "A1");

        Action act = () => BookingTool.CancelBooking(first, first.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        second.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Fact]
    public async Task CancelBooking_ConcurrentCommitDoesNotLeaveSeatsOccupied()
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var booking = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        using var barrier = new Barrier(2);
        var cancellationToken = TestContext.Current.CancellationToken;
        var commit = Task.Run(() =>
        {
            barrier.SignalAndWait(cancellationToken);
            return Record.Exception(() => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true)));
        }, cancellationToken);
        var cancel = Task.Run(() =>
        {
            barrier.SignalAndWait(cancellationToken);
            return Record.Exception(() => BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true)));
        }, cancellationToken);

        await Task.WhenAll(commit, cancel);

        var error = await commit;
        if (error is not null)
            error.Should().BeOfType<InvalidOperationException>();
        var cancelError = await cancel;
        if (cancelError is not null)
            cancelError.Should().BeOfType<InvalidOperationException>();
        if (booking.Status == BookingStatusTypes.CONFIRMED)
            showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        else
        {
            booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
            showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        }
    }

    [Fact]
    public void CancelBooking_RejectsCancellationOfManagerCancelledShowing()
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var booking = ReceiveAndCommit(schedule, showing, "A1");
        CinemaManager.CancelShowing(schedule, showing.ShowingId, new CinemaBookingApp.Domain.Payment.ExternalPaymentSystem(true));

        Action act = () => BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void CancelBooking_RejectsNullArguments()
    {
        var (schedule, showing) = CreateShowing(DateTime.Now.AddDays(2));
        var booking = ReceiveAndCommit(schedule, showing, "A1");
        Action nullBooking = () => BookingTool.CancelBooking(null!, booking.CustomerId, new ExternalAuthenticator(true), new ExternalPaymentSystem(true));
        Action nullAuthenticator = () => BookingTool.CancelBooking(booking, booking.CustomerId, null!, new ExternalPaymentSystem(true));
        Action nullPayment = () => BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), null!);

        nullBooking.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        nullAuthenticator.Should().Throw<ArgumentNullException>().WithParameterName("authenticator");
        nullPayment.Should().Throw<ArgumentNullException>().WithParameterName("paymentSystem");
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Authenticator_ReturnsConfiguredOutcome(bool approved)
    {
        var authenticator = new ExternalAuthenticator(approved);
        authenticator.Authenticate(Guid.NewGuid()).Should().Be(approved);
    }

    private static (Schedule Schedule, Showing Showing) CreateShowing(DateTime start)
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2", "A3" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        if (start > DateTime.Now)
        {
            var id = Guid.NewGuid();
            CinemaManager.AddShowing(catalog, schedule, room, id, film, start, 12.50m);
            return (schedule, schedule.GetShowing(id)!);
        }

        var showing = new Showing(Guid.NewGuid(), film, start, 12.50m, room);
        return (schedule, showing);
    }

    private static CinemaBooking ReceiveAndCommit(Schedule schedule, Showing showing, params string[] seats)
    {
        var booking = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), seats).Booking!;
        BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));
        return booking;
    }
}
