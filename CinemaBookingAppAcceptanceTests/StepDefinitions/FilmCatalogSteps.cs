using CinemaBookingApp.Application.Actors;
using CinemaBookingApp.Domain.Films;
using FluentAssertions;
using Reqnroll;

namespace CinemaBookingAppAcceptanceTests.StepDefinitions;

[Binding]
[Scope(Feature = "Film catalog")]
public sealed class FilmCatalogSteps
{
    private readonly FilmCatalog _catalog = new();
    private Exception? _error;
    private string _title = "";
    private Genre _genre;
    private int _duration;

    [Given("the film catalog is empty")]
    public void EmptyCatalog() => _catalog.GetAllMovies().Should().BeEmpty();

    [When("the manager creates a film with title {string}, genre {string}, and duration {int} minutes")]
    public void CreateFilm(string title, string genre, int duration)
    {
        _title = title;
        _genre = Enum.TryParse<Genre>(genre.Replace(" ", ""), true, out var parsed) ? parsed : (Genre)(-1);
        _duration = duration;
        _error = Record.Exception(() => Manager.CreateFilm(_catalog, title, _genre, TimeSpan.FromMinutes(duration)));
    }

    [Then("the film is added to the catalog")]
    public void FilmIsAdded()
    {
        _error.Should().BeNull();
        var film = _catalog.GetAllMovies().Should().ContainSingle().Subject;
        film.Title.Should().Be(_title);
        film.Genre.Should().Be(_genre);
        film.Duration.Should().Be(TimeSpan.FromMinutes(_duration));
    }

    [Then("the film has a unique identifier")]
    public void FilmHasId()
    {
        var film = _catalog.GetAllMovies().Single();
        film.MovieId.Should().NotBeEmpty();
        _catalog.TryGetMovie(film.MovieId, out var found).Should().BeTrue();
        found.Should().BeSameAs(film);
        Manager.CreateFilm(_catalog, "Another film", Genre.Drama, TimeSpan.FromMinutes(90));
        var ids = _catalog.GetAllMovies().Select(item => item.MovieId).ToArray();
        ids.Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Then("the film is rejected as invalid")]
    public void FilmIsRejected() => _error.Should().BeAssignableTo<ArgumentException>();

    [Then("the film catalog remains empty")]
    public void CatalogRemainsEmpty() => _catalog.GetAllMovies().Should().BeEmpty();
}
