using CinemaBookingApp.Domain.Films;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Films;

public class FilmCatalogTests
{
    [Fact]
    public void GetAllMovies_ShouldReturnEmptyCollectionForAnEmptyLibrary()
    {
        var library = new FilmCatalog();

        var movies = library.GetAllMovies();

        movies.Should().BeEmpty();
    }

    [Fact]
    public void GetAllMovies_ShouldReturnAllMoviesStillInTheLibrary()
    {
        var library = new FilmCatalog();
        var first = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var second = new Film(Guid.NewGuid(), "Other", Genre.Drama, TimeSpan.FromMinutes(100));
        var removed = new Film(Guid.NewGuid(), "Removed", Genre.Comedy, TimeSpan.FromMinutes(90));
        library.AddMovie(first);
        library.AddMovie(second);
        library.AddMovie(removed);
        library.RemoveMovie(removed.MovieId);

        var movies = library.GetAllMovies();

        movies.Should().BeEquivalentTo([first, second]);
    }

    [Fact]
    public void TryGetMovie_ShouldFindAnAddedMovieByItsIdentifier()
    {
        var library = new FilmCatalog();
        var movie = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));

        library.AddMovie(movie);
        var found = library.TryGetMovie(movie.MovieId, out var result);

        found.Should().BeTrue();
        result.Should().BeSameAs(movie);
    }

    [Fact]
    public void TryGetMovie_ShouldDistinguishMoviesByIdentifier()
    {
        var library = new FilmCatalog();
        var first = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var second = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        library.AddMovie(first);
        library.AddMovie(second);

        var foundFirst = library.TryGetMovie(first.MovieId, out var firstResult);
        var foundSecond = library.TryGetMovie(second.MovieId, out var secondResult);

        first.MovieId.Should().NotBe(second.MovieId);
        foundFirst.Should().BeTrue();
        firstResult.Should().BeSameAs(first);
        foundSecond.Should().BeTrue();
        secondResult.Should().BeSameAs(second);
    }

    [Fact]
    public void TryGetMovie_ShouldReturnFalseForAnUnknownIdentifier()
    {
        var library = new FilmCatalog();

        var found = library.TryGetMovie(Guid.NewGuid(), out var result);

        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void AddMovie_ShouldReplaceMovieWithSameIdentifier()
    {
        var library = new FilmCatalog();
        var id = Guid.NewGuid();
        var replacement = new Film(id, "Second", Genre.Drama, TimeSpan.FromMinutes(100));
        library.AddMovie(new Film(id, "First", Genre.Comedy, TimeSpan.FromMinutes(90)));
        library.AddMovie(replacement);

        library.GetAllMovies().Should().ContainSingle().Which.Should().BeSameAs(replacement);
    }

    [Fact]
    public void AddMovie_ShouldRejectNull()
    {
        var library = new FilmCatalog();
        Action act = () => library.AddMovie(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("movie");
        library.GetAllMovies().Should().BeEmpty();
    }

    [Fact]
    public void RemoveMovie_ShouldIgnoreUnknownIdentifier()
    {
        var library = new FilmCatalog();
        var film = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        library.AddMovie(film);

        library.RemoveMovie(Guid.NewGuid());

        library.GetAllMovies().Should().ContainSingle().Which.Should().BeSameAs(film);
    }

    [Fact]
    public void RemoveMovie_ShouldRemoveOnlyTheSpecifiedMovie()
    {
        var library = new FilmCatalog();
        var removed = new Film(Guid.NewGuid(), "Arrival", Genre.ScienceFiction, TimeSpan.FromMinutes(116));
        var remaining = new Film(Guid.NewGuid(), "Other", Genre.Drama, TimeSpan.FromMinutes(100));
        library.AddMovie(removed);
        library.AddMovie(remaining);

        library.RemoveMovie(removed.MovieId);

        library.TryGetMovie(removed.MovieId, out var removedResult).Should().BeFalse();
        removedResult.Should().BeNull();
        library.TryGetMovie(remaining.MovieId, out var remainingResult).Should().BeTrue();
        remainingResult.Should().BeSameAs(remaining);
    }
}
