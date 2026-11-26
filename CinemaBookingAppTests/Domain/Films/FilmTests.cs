using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Films;

public class FilmTests
{
    [Fact]
    public void Constructor_ShouldPreserveValidMovieDetails()
    {
        var id = Guid.NewGuid();

        var movie = new Film(id, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        movie.MovieId.Should().Be(id);
        movie.Title.Should().Be("Arrival");
        movie.Genre.Should().Be(Genre.ScienceFiction);
        movie.Duration.Should().Be(TimeSpan.FromMinutes(116));
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyTitle()
    {
        Action act = () => new Film(Guid.NewGuid(), "", Genre.Drama, TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldRejectNullTitle()
    {
        Action act = () => new Film(Guid.NewGuid(), null!, Genre.Drama, TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldRejectAnUndefinedGenre()
    {
        Action act = () => new Film(Guid.NewGuid(), "Arrival", (Genre)(-1), TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_ShouldRejectNonPositiveDuration(int minutes)
    {
        Action act = () => new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(minutes));

        act.Should().Throw<ArgumentException>();
    }
}
