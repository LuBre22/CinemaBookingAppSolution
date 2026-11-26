using CinemaBookingApp.Application.ShowRooms;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.ShowRooms;

public class ShowingTests
{
    [Fact]
    public void Constructor_ShouldCreateShowingWithCalculatedEndTime()
    {
        var id = Guid.NewGuid();
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var start = new DateTime(2026, 11, 26, 23, 0, 0, DateTimeKind.Utc);

        var showing = new Showing(id, film, start, 12.50m);

        showing.ShowingId.Should().Be(id);
        showing.Film.Should().BeSameAs(film);
        showing.StartingTime.Should().Be(start);
        showing.EndingTime.Should().Be(new DateTime(2026, 11, 27, 0, 56, 0, DateTimeKind.Utc));
        showing.EndingTime.Kind.Should().Be(DateTimeKind.Utc);
        showing.TicketPrice.Should().Be(12.50m);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyShowingId()
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        Action act = () => new Showing(Guid.Empty, film, new DateTime(2026, 11, 26, 12, 0, 0), 12.50m);

        act.Should().Throw<ArgumentException>().WithParameterName("showingId");
    }

    [Fact]
    public void Constructor_ShouldRejectNullFilm()
    {
        Action act = () => new Showing(Guid.NewGuid(), null!, new DateTime(2026, 11, 26, 12, 0, 0), 12.50m);

        act.Should().Throw<ArgumentNullException>().WithParameterName("film");
    }

    [Fact]
    public void Constructor_ShouldRejectEndTimeOverflow()
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        Action act = () => new Showing(Guid.NewGuid(), film, DateTime.MaxValue, 12.50m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectNonPositiveTicketPrice(int price)
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        Action act = () => new Showing(Guid.NewGuid(), film, new DateTime(2026, 11, 26, 12, 0, 0), price);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("ticketPrice");
    }

    [Fact]
    public void Constructor_ShouldRejectTicketPriceWithMoreThanTwoDecimalPlaces()
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        Action act = () => new Showing(Guid.NewGuid(), film, new DateTime(2026, 11, 26, 12, 0, 0), 12.345m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("ticketPrice");
    }

    [Fact]
    public void Constructor_ShouldAcceptAnIntegerTicketPrice()
    {
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        var showing = new Showing(Guid.NewGuid(), film, new DateTime(2026, 11, 26, 12, 0, 0), 12m);

        showing.TicketPrice.Should().Be(12m);
    }
}
