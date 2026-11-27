using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FluentAssertions;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;
using CinemaProgram = CinemaBookingApp.Application.Scheduler.Program;

namespace CinemaBookingAppTests.Application.Booking;

public class CommitBookingTests
{
    [Fact]
    public void CommitBooking_ShouldOccupySelectedSeatsOnlyForTheShowing()
    {
        var (schedule, showing) = CreateShowing();
        var otherId = Guid.NewGuid();
        var catalog = new FilmCatalog();
        catalog.AddMovie(showing.Film);
        CinemaManager.AddShowing(catalog, schedule, showing.Room, otherId, showing.Film, showing.StartingTime.AddDays(1), 12.50m);
        var booking = ReceiveAndConfirm(schedule, showing, "A1", "A2");

        BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        showing.Seats["A3"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        schedule.GetShowing(otherId)!.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
        CinemaProgram.GetProgram(schedule, showing.StartingTime, showing.StartingTime.AddDays(2))
            .Single(line => line.ShowingId == showing.ShowingId).AvailableSeats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Status
            .Should().Be(BookingStatusTypes.DENIED);
    }

    [Theory]
    [InlineData(BookingStatusTypes.CONFIRMED)]
    [InlineData(BookingStatusTypes.DENIED)]
    [InlineData(BookingStatusTypes.CANCELLED)]
    public void CommitBooking_ShouldRejectOtherStatuses(BookingStatusTypes status)
    {
        var (schedule, showing) = CreateShowing();
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, status);

        Action act = () => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(status);
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void CommitBooking_ShouldDenyConflictingBookingWithoutReservingAnyOtherSeats()
    {
        var (schedule, showing) = CreateShowing();
        var first = ReceiveAndConfirm(schedule, showing, "A1");
        var second = ReceiveAndConfirm(schedule, showing, "A1", "A2");
        BookingTool.CommitBooking(schedule, first, new ExternalPaymentSystem(true));

        Action act = () => BookingTool.CommitBooking(schedule, second, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        second.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void CommitBooking_ShouldRejectRepeatWithoutDenyingCommittedBooking()
    {
        var (schedule, showing) = CreateShowing();
        var booking = ReceiveAndConfirm(schedule, showing, "A1");
        BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        Action act = () => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommitBooking_ShouldDenyCancelledOrStartedShowing(bool cancelled)
    {
        var (schedule, showing) = CreateShowing();
        CinemaBooking booking;
        if (cancelled)
        {
            booking = ReceiveAndConfirm(schedule, showing, "A1");
            CinemaManager.CancelShowing(schedule, showing.ShowingId, new CinemaBookingApp.Domain.Payment.ExternalPaymentSystem(true));
        }
        else
        {
            var old = new Showing(Guid.NewGuid(), showing.Film, DateTime.Now.AddHours(-1), 12.50m, showing.Room);
            showing.Room.AddShowing(old);
            schedule.AddShowing(showing.Room, old);
            showing = old;
            booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.RECEIVED);
        }

        Action act = () => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(cancelled ? BookingStatusTypes.CANCELLED : BookingStatusTypes.DENIED);
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Theory]
    [InlineData("Z99")]
    [InlineData("")]
    public void CommitBooking_ShouldDenyInvalidSelectionWithoutPartialReservation(string invalid)
    {
        var (schedule, showing) = CreateShowing();
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1", invalid }, BookingStatusTypes.RECEIVED);

        Action act = () => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void CommitBooking_ShouldRejectMismatchedShowingEvenWithSameId()
    {
        var (schedule, showing) = CreateShowing();
        var other = new Showing(showing.ShowingId, showing.Film, showing.StartingTime, 12.50m, showing.Room);
        var booking = new CinemaBooking(other, Guid.NewGuid(), new[] { "A1" }, BookingStatusTypes.RECEIVED);

        Action act = () => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));

        act.Should().Throw<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void CommitBooking_ShouldDenyEmptyOrDuplicateSelections()
    {
        var (schedule, showing) = CreateShowing();
        var empty = new CinemaBooking(showing, Guid.NewGuid(), Array.Empty<string>(), BookingStatusTypes.RECEIVED);
        var duplicate = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1", "A1" }, BookingStatusTypes.RECEIVED);

        Action emptyCommit = () => BookingTool.CommitBooking(schedule, empty, new ExternalPaymentSystem(true));
        Action duplicateCommit = () => BookingTool.CommitBooking(schedule, duplicate, new ExternalPaymentSystem(true));

        emptyCommit.Should().Throw<InvalidOperationException>();
        duplicateCommit.Should().Throw<InvalidOperationException>();
        empty.Status.Should().Be(BookingStatusTypes.DENIED);
        duplicate.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats.Values.Should().OnlyContain(value => value == SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public async Task CommitBooking_ShouldAllowOnlyOneConcurrentReservationForTheSameSeat()
    {
        var (schedule, showing) = CreateShowing();
        var first = ReceiveAndConfirm(schedule, showing, "A1");
        var second = ReceiveAndConfirm(schedule, showing, "A1");
        using var barrier = new Barrier(2);
        Task<Exception?> Commit(CinemaBooking booking) => Task.Run(() =>
        {
            barrier.SignalAndWait();
            return Record.Exception(() => BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true)));
        });

        var outcomes = await Task.WhenAll(Commit(first), Commit(second));

        outcomes.Count(error => error is null).Should().Be(1);
        outcomes.Count(error => error is InvalidOperationException).Should().Be(1);
        new[] { first.Status, second.Status }.Should().BeEquivalentTo(
            new[] { BookingStatusTypes.CONFIRMED, BookingStatusTypes.DENIED });
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Fact]
    public void CommitBooking_ShouldRejectNullArguments()
    {
        var (schedule, showing) = CreateShowing();
        var booking = ReceiveAndConfirm(schedule, showing, "A1");
        Action noSchedule = () => BookingTool.CommitBooking(null!, booking, new ExternalPaymentSystem(true));
        Action noBooking = () => BookingTool.CommitBooking(schedule, null!, new ExternalPaymentSystem(true));

        noSchedule.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
        noBooking.Should().Throw<ArgumentNullException>().WithParameterName("booking");
        booking.Status.Should().Be(BookingStatusTypes.RECEIVED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    private static (Schedule Schedule, Showing Showing) CreateShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2", "A3" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var id = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, room, id, film, DateTime.Today.AddDays(2).AddHours(12), 12.50m);
        return (schedule, schedule.GetShowing(id)!);
    }

    private static CinemaBooking ReceiveAndConfirm(Schedule schedule, Showing showing, params string[] seats)
    {
        var booking = BookingTool.Receive(new Bookings(new ExternalAuthenticator(true)), schedule, showing.ShowingId, Guid.NewGuid(), seats).Booking!;
        return booking;
    }
}
