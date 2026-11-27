using CinemaBookingApp.Application.Booking;
using CinemaBookingApp.Application.Scheduler;
using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Authentification;
using CinemaBookingApp.Domain.Films;
using CinemaBookingApp.Domain.Payment;
using FakeItEasy;
using FluentAssertions;
using CinemaManager = CinemaBookingApp.Application.Actors.Manager;
using CinemaBooking = CinemaBookingApp.Application.Booking.Booking;

namespace CinemaBookingAppTests.Application.Booking;

public class BookingLifecycleTests
{
    [Fact]
    public void Commit_VerifiesPaymentBeforeConfirmingAndReserving()
    {
        var (schedule, showing, bookings) = Create();
        var received = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        BookingTool.CommitBooking(schedule, received, new ExternalPaymentSystem(true));
        received.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);

        var denied = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A2" }).Booking!;
        BookingTool.CommitBooking(schedule, denied, new ExternalPaymentSystem(false));
        denied.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Cancel_RefundsAndSetsCancelled()
    {
        var (schedule, showing, bookings) = Create();
        var booking = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(A<CinemaBooking>._)).Returns(true);
        BookingTool.CommitBooking(schedule, booking, payment);
        BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), payment);
        booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        A.CallTo(() => payment.Refund(booking)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void ManagerCancellation_RefundsReceivedAndConfirmedAcrossHoldersOnlyForThatShowing()
    {
        var (schedule, showing, bookings) = Create();
        var secondHolder = new Bookings(new ExternalAuthenticator(true));
        var confirmed = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var received = BookingTool.Receive(secondHolder, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A2" }).Booking!;
        var denied = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A2" }).Booking!;
        var previouslyCancelled = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A2" }).Booking!;
        var filmCatalog = new FilmCatalog();
        filmCatalog.AddMovie(showing.Film);
        var otherId = Guid.NewGuid();
        CinemaManager.AddShowing(filmCatalog, schedule, showing.Room, otherId, showing.Film, showing.StartingTime.AddDays(1), 12m);
        var otherShowing = BookingTool.Receive(bookings, schedule, otherId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(A<CinemaBooking>._)).Returns(true);
        BookingTool.CommitBooking(schedule, confirmed, payment);
        BookingTool.CommitBooking(schedule, denied, new ExternalPaymentSystem(false));
        BookingTool.CommitBooking(schedule, previouslyCancelled, payment);
        BookingTool.CancelBooking(previouslyCancelled, previouslyCancelled.CustomerId, new ExternalAuthenticator(true), payment);
        BookingTool.CommitBooking(schedule, otherShowing, payment);

        CinemaManager.CancelShowing(schedule, showing.ShowingId, payment);

        confirmed.Status.Should().Be(BookingStatusTypes.CANCELLED);
        received.Status.Should().Be(BookingStatusTypes.CANCELLED);
        denied.Status.Should().Be(BookingStatusTypes.DENIED);
        previouslyCancelled.Status.Should().Be(BookingStatusTypes.CANCELLED);
        otherShowing.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        schedule.GetShowing(otherId)!.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        A.CallTo(() => payment.Refund(confirmed)).MustHaveHappenedOnceExactly();
        A.CallTo(() => payment.Refund(received)).MustHaveHappenedOnceExactly();
        A.CallTo(() => payment.Refund(denied)).MustNotHaveHappened();
        A.CallTo(() => payment.Refund(previouslyCancelled)).MustHaveHappenedOnceExactly();
        A.CallTo(() => payment.Refund(otherShowing)).MustNotHaveHappened();
        bookings.ViewMyBookings(confirmed.CustomerId).Should().ContainSingle(b => b.Status == BookingStatusTypes.CANCELLED);
        Action repeat = () => BookingTool.CancelBooking(confirmed, confirmed.CustomerId, new ExternalAuthenticator(true), payment);
        repeat.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ViewMyBookings_SortsByShowingStartRatherThanInsertion()
    {
        var (schedule, first, bookings) = Create();
        var catalog = new FilmCatalog();
        catalog.AddMovie(first.Film);
        var laterId = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, first.Room, laterId, first.Film, first.StartingTime.AddDays(1), 12m);
        var customer = Guid.NewGuid();
        var later = BookingTool.Receive(bookings, schedule, laterId, customer, new[] { "A1" }).Booking!;
        var earlier = BookingTool.Receive(bookings, schedule, first.ShowingId, customer, new[] { "A1" }).Booking!;
        bookings.ViewMyBookings(customer).Should().Equal(earlier, later);
    }

    [Theory]
    [InlineData(BookingStatusTypes.RECEIVED)]
    [InlineData(BookingStatusTypes.CONFIRMED)]
    public void Add_RejectsActiveBookingForCancelledShowing(BookingStatusTypes status)
    {
        var (schedule, showing, bookings) = Create();
        CinemaManager.CancelShowing(schedule, showing.ShowingId, new ExternalPaymentSystem(true));
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, status);
        Action add = () => bookings.Add(booking);
        add.Should().Throw<InvalidOperationException>();
        bookings.ViewMyBookings(booking.CustomerId).Should().BeEmpty();
    }

    [Theory]
    [InlineData(BookingStatusTypes.DENIED)]
    [InlineData(BookingStatusTypes.CANCELLED)]
    public void Add_RetainsInactiveHistoryForCancelledShowing(BookingStatusTypes status)
    {
        var (schedule, showing, bookings) = Create();
        CinemaManager.CancelShowing(schedule, showing.ShowingId, new ExternalPaymentSystem(true));
        var booking = new CinemaBooking(showing, Guid.NewGuid(), new[] { "A1" }, status);
        bookings.Add(booking);
        bookings.ViewMyBookings(booking.CustomerId).Should().Equal(booking);
    }

    [Fact]
    public void Add_SameBookingInDifferentHoldersIsCancelledAndRefundedOnlyOnce()
    {
        var (schedule, showing, bookings) = Create();
        var secondHolder = new Bookings(new ExternalAuthenticator(true));
        var booking = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        secondHolder.Add(booking);
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(A<CinemaBooking>._)).Returns(true);
        CinemaManager.CancelShowing(schedule, showing.ShowingId, payment);
        booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
        A.CallTo(() => payment.Refund(booking)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Cancel_RefundFailureLeavesBookingAndSeatUnchanged()
    {
        var (schedule, showing, bookings) = Create();
        var booking = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        BookingTool.CommitBooking(schedule, booking, new ExternalPaymentSystem(true));
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(booking)).Returns(false);
        Action cancel = () => BookingTool.CancelBooking(booking, booking.CustomerId, new ExternalAuthenticator(true), payment);
        cancel.Should().Throw<InvalidOperationException>().WithMessage("*refund failed*");
        booking.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
    }

    [Fact]
    public void Commit_RefundFailureAfterReservationConflictIsReported()
    {
        var (schedule, showing, bookings) = Create();
        var owner = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var competing = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1", "A2" }).Booking!;
        BookingTool.CommitBooking(schedule, owner, new ExternalPaymentSystem(true));
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(competing)).Returns(false);
        Action commit = () => BookingTool.CommitBooking(schedule, competing, payment);
        commit.Should().Throw<InvalidOperationException>().WithMessage("*refund failed*");
        competing.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    [Fact]
    public void Commit_RefundsApprovedPaymentIfReservationFails()
    {
        var (schedule, showing, bookings) = Create();
        var first = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var second = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1", "A2" }).Booking!;
        BookingTool.CommitBooking(schedule, first, new ExternalPaymentSystem(true));
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(second)).Returns(true);

        Action commit = () => BookingTool.CommitBooking(schedule, second, payment);

        commit.Should().Throw<InvalidOperationException>();
        second.Status.Should().Be(BookingStatusTypes.DENIED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.OCCUPIED);
        showing.Seats["A2"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        A.CallTo(() => payment.Refund(second)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CancelShowing_RetriesFailedRefundWithoutRefundingCompletedCancellationsTwice()
    {
        var (schedule, showing, bookings) = Create();
        var first = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var second = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A2" }).Booking!;
        BookingTool.CommitBooking(schedule, first, new ExternalPaymentSystem(true));
        BookingTool.CommitBooking(schedule, second, new ExternalPaymentSystem(true));
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(first)).Returns(true);
        A.CallTo(() => payment.Refund(second)).ReturnsNextFromSequence(false, true);

        Action cancel = () => CinemaManager.CancelShowing(schedule, showing.ShowingId, payment);
        cancel.Should().Throw<InvalidOperationException>().WithMessage("*refund failed*");
        showing.Status.Should().Be(FilmStatusCategories.CANCELLED);
        first.Status.Should().Be(BookingStatusTypes.CANCELLED);
        second.Status.Should().Be(BookingStatusTypes.CONFIRMED);
        Action customerCancel = () => BookingTool.CancelBooking(second, second.CustomerId, new ExternalAuthenticator(true), payment);
        customerCancel.Should().Throw<InvalidOperationException>();

        CinemaManager.CancelShowing(schedule, showing.ShowingId, payment);

        second.Status.Should().Be(BookingStatusTypes.CANCELLED);
        schedule.GetShowing(showing.ShowingId).Should().BeNull();
        A.CallTo(() => payment.Refund(first)).MustHaveHappenedOnceExactly();
        A.CallTo(() => payment.Refund(second)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task CancelShowing_RacingWithCommitCancelsAndRefundsBookingExactlyOnce()
    {
        var (schedule, showing, bookings) = Create();
        var booking = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        var payment = A.Fake<ExternalPaymentSystem>(x => x.WithArgumentsForConstructor(() => new ExternalPaymentSystem(true)));
        A.CallTo(() => payment.Refund(booking)).Returns(true);
        using var barrier = new Barrier(2);
        var token = TestContext.Current.CancellationToken;
        var commit = Task.Run(() =>
        {
            barrier.SignalAndWait(token);
            return Record.Exception(() => BookingTool.CommitBooking(schedule, booking, payment));
        }, token);
        var cancel = Task.Run(() =>
        {
            barrier.SignalAndWait(token);
            CinemaManager.CancelShowing(schedule, showing.ShowingId, payment);
        }, token);

        await Task.WhenAll(commit, cancel);

        var error = await commit;
        if (error is not null)
            error.Should().BeOfType<InvalidOperationException>();
        booking.Status.Should().Be(BookingStatusTypes.CANCELLED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
        A.CallTo(() => payment.Refund(booking)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CancelShowing_RejectsNullPayment()
    {
        var (schedule, showing, _) = Create();
        Action act = () => CinemaManager.CancelShowing(schedule, showing.ShowingId, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("paymentSystem");
        showing.Status.Should().Be(FilmStatusCategories.PLANNED);
    }

    [Fact]
    public void Commit_RejectsNullPayment()
    {
        var (schedule, showing, bookings) = Create();
        var booking = BookingTool.Receive(bookings, schedule, showing.ShowingId, Guid.NewGuid(), new[] { "A1" }).Booking!;
        Action act = () => BookingTool.CommitBooking(schedule, booking, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("paymentSystem");
        booking.Status.Should().Be(BookingStatusTypes.RECEIVED);
        showing.Seats["A1"].Should().Be(SeatAvailabilityTypes.AVAILABLE);
    }

    private static (Schedule, Showing, Bookings) Create()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), new[] { "A1", "A2" });
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.Drama, TimeSpan.FromMinutes(60));
        var catalog = new FilmCatalog();
        catalog.AddMovie(film);
        var schedule = new Schedule();
        var id = Guid.NewGuid();
        CinemaManager.AddShowing(catalog, schedule, room, id, film, DateTime.Today.AddDays(2), 12m);
        return (schedule, schedule.GetShowing(id)!, new Bookings(new ExternalAuthenticator(true)));
    }
}
