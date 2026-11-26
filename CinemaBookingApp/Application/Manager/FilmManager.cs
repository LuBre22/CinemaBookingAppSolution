using CinemaBookingApp.Domain.Films;

namespace CinemaBookingApp.Application.Manager;

public sealed class FilmManager
{
    public void CreateFilm(FilmCatalog catalog, string name, Genre genre, TimeSpan duration)
    {
        var film = new Film(Guid.NewGuid(), name, genre, duration);
        catalog.AddMovie(film);
    }   
}