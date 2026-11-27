using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.ShowRooms;

public class RoomTests
{
    [Fact]
    public void ShowingMutations_ShouldNotBePublic()
    {
        typeof(Room).GetMethods()
            .Select(method => method.Name)
            .Should().NotContain(new[] { "AddShowing", "RemoveShowing" });
    }

    [Fact]
    public void GetOrCreate_WithSeats_ShouldDeriveCapacity()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));

        room.Capacity.Should().Be(100);
    }

    [Fact]
    public void GetOrCreate_WithSeats_ShouldUpdateExistingEmptyRoom()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, TestSeatIds.Create(100));
        var updated = Room.GetOrCreate(id, TestSeatIds.Create(150));

        updated.Should().BeSameAs(room);
        room.Capacity.Should().Be(150);
        room.Showings.Should().BeEmpty();
        Room.GetOrCreate(id).Capacity.Should().Be(150);
    }

    [Fact]
    public void GetOrCreate_WithoutSeats_ShouldRejectUnknownRoom()
    {
        var id = Guid.NewGuid();
        Action act = () => Room.GetOrCreate(id);

        act.Should().Throw<KeyNotFoundException>();
        Room.GetOrCreate(id, TestSeatIds.Create(100)).Capacity.Should().Be(100);
    }

    [Fact]
    public void GetOrCreate_WithSeats_ShouldRejectEmptySeatLayout()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, TestSeatIds.Create(100));

        Action update = () => Room.GetOrCreate(id, Array.Empty<string>());
        Action create = () => Room.GetOrCreate(Guid.NewGuid(), Array.Empty<string>());

        update.Should().Throw<ArgumentException>().WithParameterName("seatIds");
        create.Should().Throw<ArgumentException>().WithParameterName("seatIds");
        room.Capacity.Should().Be(100);
    }

    [Fact]
    public void GetOrCreate_WithSeats_ShouldRejectEmptyRoomNumber()
    {
        Action act = () => Room.GetOrCreate(Guid.Empty, TestSeatIds.Create(100));

        act.Should().Throw<ArgumentException>().WithParameterName("roomNumber");
    }

    [Fact]
    public void GetOrCreate_ShouldReturnSameRoomForSameId()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, TestSeatIds.Create(100));

        Room.GetOrCreate(id).Should().BeSameAs(room);
        room.RoomNumber.Should().Be(id);
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void GetOrCreate_ShouldReturnDifferentRoomsForDifferentIds()
    {
        Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100)).Should().NotBeSameAs(Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100)));
    }

    [Fact]
    public void GetOrCreate_ShouldRejectEmptyRoomNumber()
    {
        Action act = () => Room.GetOrCreate(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithParameterName("roomNumber");
    }

    [Fact]
    public async Task GetOrCreate_ShouldReturnSameRoomAcrossConcurrentCalls()
    {
        var id = Guid.NewGuid();
        var rooms = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Room.GetOrCreate(id, TestSeatIds.Create(100)))));

        rooms.Should().OnlyContain(room => ReferenceEquals(room, rooms[0]));
    }

    [Fact]
    public void AddShowing_ShouldRetainShowingsAcrossDaysAndLookups()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, TestSeatIds.Create(100));
        var first = CreateShowing(room, new DateTime(2030, 1, 1, 12, 0, 0));
        var second = CreateShowing(room, new DateTime(2030, 1, 2, 12, 0, 0));

        room.AddShowing(first);
        Room.GetOrCreate(id).AddShowing(second);

        room.Showings.Should().Equal(first, second);
    }

    [Fact]
    public void Showings_ShouldNotAllowExternalModification()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var list = (ICollection<Showing>)room.Showings;

        Action act = () => list.Add(CreateShowing(room, new DateTime(2030, 1, 1, 12, 0, 0)));

        act.Should().Throw<NotSupportedException>();
        room.Showings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(60)]
    [InlineData(145)]
    public void AddShowing_ShouldRejectOverlapOrInsufficientBuffer(int minutes)
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var start = new DateTime(2030, 1, 1, 12, 0, 0);
        room.AddShowing(CreateShowing(room, start));

        Action act = () => room.AddShowing(CreateShowing(room, start.AddMinutes(minutes)));

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-146)]
    [InlineData(146)]
    public void AddShowing_ShouldAcceptThirtyMinuteBuffer(int minutes)
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var start = new DateTime(2030, 1, 1, 12, 0, 0);
        room.AddShowing(CreateShowing(room, start));

        room.AddShowing(CreateShowing(room, start.AddMinutes(minutes)));

        room.Showings.Should().HaveCount(2);
    }

    [Fact]
    public void AddShowing_ShouldRejectNull()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        Action act = () => room.AddShowing(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("showing");
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void AddShowing_ShouldRejectCancelledShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var showing = CreateShowing(room, DateTime.Today.AddDays(2));
        showing.Cancel();

        Action act = () => room.AddShowing(showing);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void RegisterScheduledShowing_ShouldRejectShowingNotInRoom()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var showing = CreateShowing(room, DateTime.Today.AddDays(2));

        Action act = () => room.RegisterScheduledShowing(showing);

        act.Should().Throw<ArgumentException>().WithParameterName("showing");
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void RegisterScheduledShowing_ShouldRejectDuplicateRegistration()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var showing = CreateShowing(room, DateTime.Today.AddDays(2));
        room.AddShowing(showing);
        room.RegisterScheduledShowing(showing);

        Action act = () => room.RegisterScheduledShowing(showing);

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    [Fact]
    public void RemoveShowing_ShouldRemoveOnlyMatchingShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var removed = CreateShowing(room, new DateTime(2030, 1, 1, 12, 0, 0));
        var remaining = CreateShowing(room, new DateTime(2030, 1, 2, 12, 0, 0));
        room.AddShowing(removed);
        room.AddShowing(remaining);

        room.RemoveShowing(removed.ShowingId);

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(remaining);
    }

    [Fact]
    public void RemoveShowing_ShouldLeaveRoomUnchangedForUnknownId()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), TestSeatIds.Create(100));
        var showing = CreateShowing(room, new DateTime(2030, 1, 1, 12, 0, 0));
        room.AddShowing(showing);

        room.RemoveShowing(Guid.NewGuid());

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    private static Showing CreateShowing(Room room, DateTime start)
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        return new Showing(Guid.NewGuid(), film, start, 12.50m, room);
    }
}
