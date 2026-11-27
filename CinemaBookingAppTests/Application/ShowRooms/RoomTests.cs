using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.ShowRooms;

public class RoomTests
{
    [Fact]
    public void GetOrCreate_WithCapacity_ShouldCreateRoomWithCapacity()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);

        room.Capacity.Should().Be(100);
    }

    [Fact]
    public void GetOrCreate_WithCapacity_ShouldUpdateExistingRoomWithoutLosingShowings()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, 100);
        var showing = CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0));
        room.AddShowing(showing);

        var updated = Room.GetOrCreate(id, 150);

        updated.Should().BeSameAs(room);
        room.Capacity.Should().Be(150);
        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
        Room.GetOrCreate(id).Capacity.Should().Be(150);
    }

    [Fact]
    public void GetOrCreate_WithoutCapacity_ShouldRejectUnknownRoom()
    {
        var id = Guid.NewGuid();
        Action act = () => Room.GetOrCreate(id);

        act.Should().Throw<KeyNotFoundException>();
        Room.GetOrCreate(id, 100).Capacity.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetOrCreate_WithCapacity_ShouldRejectNonPositiveCapacity(int capacity)
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, 100);

        Action update = () => Room.GetOrCreate(id, capacity);
        Action create = () => Room.GetOrCreate(Guid.NewGuid(), capacity);

        update.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("capacity");
        create.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("capacity");
        room.Capacity.Should().Be(100);
    }

    [Fact]
    public void GetOrCreate_WithCapacity_ShouldRejectEmptyRoomNumber()
    {
        Action act = () => Room.GetOrCreate(Guid.Empty, 100);

        act.Should().Throw<ArgumentException>().WithParameterName("roomNumber");
    }

    [Fact]
    public void GetOrCreate_ShouldReturnSameRoomForSameId()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, 100);

        Room.GetOrCreate(id).Should().BeSameAs(room);
        room.RoomNumber.Should().Be(id);
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void GetOrCreate_ShouldReturnDifferentRoomsForDifferentIds()
    {
        Room.GetOrCreate(Guid.NewGuid(), 100).Should().NotBeSameAs(Room.GetOrCreate(Guid.NewGuid(), 100));
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
        var rooms = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Room.GetOrCreate(id, 100))));

        rooms.Should().OnlyContain(room => ReferenceEquals(room, rooms[0]));
    }

    [Fact]
    public void AddShowing_ShouldRetainShowingsAcrossDaysAndLookups()
    {
        var id = Guid.NewGuid();
        var room = Room.GetOrCreate(id, 100);
        var first = CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0));
        var second = CreateShowing(new DateTime(2030, 1, 2, 12, 0, 0));

        room.AddShowing(first);
        Room.GetOrCreate(id).AddShowing(second);

        room.Showings.Should().Equal(first, second);
    }

    [Fact]
    public void Showings_ShouldNotAllowExternalModification()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var list = (ICollection<Showing>)room.Showings;

        Action act = () => list.Add(CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0)));

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
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var start = new DateTime(2030, 1, 1, 12, 0, 0);
        room.AddShowing(CreateShowing(start));

        Action act = () => room.AddShowing(CreateShowing(start.AddMinutes(minutes)));

        act.Should().Throw<InvalidOperationException>();
        room.Showings.Should().ContainSingle();
    }

    [Theory]
    [InlineData(-146)]
    [InlineData(146)]
    public void AddShowing_ShouldAcceptThirtyMinuteBuffer(int minutes)
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var start = new DateTime(2030, 1, 1, 12, 0, 0);
        room.AddShowing(CreateShowing(start));

        room.AddShowing(CreateShowing(start.AddMinutes(minutes)));

        room.Showings.Should().HaveCount(2);
    }

    [Fact]
    public void AddShowing_ShouldRejectNull()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        Action act = () => room.AddShowing(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("showing");
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveShowing_ShouldRemoveOnlyMatchingShowing()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var removed = CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0));
        var remaining = CreateShowing(new DateTime(2030, 1, 2, 12, 0, 0));
        room.AddShowing(removed);
        room.AddShowing(remaining);

        room.RemoveShowing(removed.ShowingId);

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(remaining);
    }

    [Fact]
    public void RemoveShowing_ShouldLeaveRoomUnchangedForUnknownId()
    {
        var room = Room.GetOrCreate(Guid.NewGuid(), 100);
        var showing = CreateShowing(new DateTime(2030, 1, 1, 12, 0, 0));
        room.AddShowing(showing);

        room.RemoveShowing(Guid.NewGuid());

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    private static Showing CreateShowing(DateTime start)
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        return new Showing(Guid.NewGuid(), film, start, 12.50m);
    }
}
