namespace CinemaBookingApp.Domain.Films;

public sealed class FilmCatalog
{
    private Dictionary<Guid, Film> _movies = new Dictionary<Guid, Film>();
    
    public void AddMovie(Film movie)
    {
        _movies[movie.MovieId] = movie;
    }
    public void RemoveMovie(Guid movieId)
    {
        _movies.Remove(movieId);
    }
    public bool TryGetMovie(Guid movieId, out Film? movie)
    {
        return _movies.TryGetValue(movieId, out movie);
    }
    public IEnumerable<Film> GetAllMovies()
    {
        return _movies.Values;
    }
}