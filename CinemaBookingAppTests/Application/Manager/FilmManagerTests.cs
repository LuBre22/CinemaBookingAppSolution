using CinemaBookingApp.Application.Manager;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Application.Manager;

public class FilmManagerTests
{
    [Fact]
    public void CreateFilm_ShouldAddFilmWithProvidedDetailsToCatalog()
    {
        var catalog = new FilmCatalog();
        var manager = new FilmManager();
        manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        var film = catalog.GetAllMovies().Should().ContainSingle().Subject;
        film.MovieId.Should().NotBe(Guid.Empty);
        film.Title.Should().Be("Arrival");
        film.Genre.Should().Be(Genre.ScienceFiction);
        film.Duration.Should().Be(TimeSpan.FromMinutes(116));
    }

    [Fact]
    public void CreateFilm_ShouldAddFilmsWithDifferentIdentifiers()
    {
        var catalog = new FilmCatalog();
        var manager = new FilmManager();
        manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        var ids = catalog.GetAllMovies().Select(film => film.MovieId).ToArray();
        ids.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        ids.Should().NotContain(Guid.Empty);
    }

    [Fact]
    public void CreateFilm_ShouldRejectEmptyTitleWithoutAddingFilm()
    {
        var catalog = new FilmCatalog();
        var manager = new FilmManager();

        Action act = () => manager.CreateFilm(catalog, "", Genre.Drama, TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Fact]
    public void CreateFilm_ShouldRejectUndefinedGenreWithoutAddingFilm()
    {
        var catalog = new FilmCatalog();
        var manager = new FilmManager();

        Action act = () => manager.CreateFilm(catalog, "Arrival", (Genre)(-1), TimeSpan.FromMinutes(100));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateFilm_ShouldRejectNonPositiveDurationWithoutAddingFilm(int minutes)
    {
        var catalog = new FilmCatalog();
        var manager = new FilmManager();

        Action act = () => manager.CreateFilm(catalog, "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(minutes));

        act.Should().Throw<ArgumentException>();
        catalog.GetAllMovies().Should().BeEmpty();
    }
}
