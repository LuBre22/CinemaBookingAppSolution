using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.ShowRooms;

public class RoomTests
{
    [Fact]
    public void Constructor_ShouldCopyShowingsAndPreserveRoomNumber()
    {
        var id = Guid.NewGuid();
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var showing = new Showing(Guid.NewGuid(), film, new DateTime(2026, 11, 26, 12, 0, 0), 12.50m);
        var showings = new List<Showing> { showing };

        var room = new Room(id, showings);
        showings.Clear();

        room.RoomNumber.Should().Be(id);
        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    [Fact]
    public void Constructor_ShouldAllowAnEmptyShowingCollection()
    {
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());

        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyRoomNumber()
    {
        Action act = () => new Room(Guid.Empty, Array.Empty<Showing>());

        act.Should().Throw<ArgumentException>().WithParameterName("roomNumber");
    }

    [Fact]
    public void Constructor_ShouldRejectNullShowings()
    {
        Action act = () => new Room(Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("showings");
    }

    [Fact]
    public void Constructor_ShouldRejectNullShowingInCollection()
    {
        Action act = () => new Room(Guid.NewGuid(), new Showing[] { null! });

        act.Should().Throw<ArgumentException>().WithParameterName("showings");
    }

    [Fact]
    public void AddShowing_ShouldAddShowingToRoom()
    {
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        var showing = CreateShowing();

        room.AddShowing(showing);

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    [Fact]
    public void AddShowing_ShouldRejectNull()
    {
        var room = new Room(Guid.NewGuid(), Array.Empty<Showing>());
        Action act = () => room.AddShowing(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("showing");
        room.Showings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveShowing_ShouldRemoveOnlyMatchingShowing()
    {
        var removed = CreateShowing();
        var remaining = CreateShowing();
        var room = new Room(Guid.NewGuid(), new[] { removed, remaining });

        room.RemoveShowing(removed.ShowingId);

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(remaining);
    }

    [Fact]
    public void RemoveShowing_ShouldLeaveRoomUnchangedForUnknownId()
    {
        var showing = CreateShowing();
        var room = new Room(Guid.NewGuid(), new[] { showing });

        room.RemoveShowing(Guid.NewGuid());

        room.Showings.Should().ContainSingle().Which.Should().BeSameAs(showing);
    }

    private static Showing CreateShowing()
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        return new Showing(Guid.NewGuid(), film, new DateTime(2030, 1, 1, 12, 0, 0), 12.50m);
    }
}
