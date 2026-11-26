namespace CinemaBookingApp.Domain.Movie;

public sealed class MovieLibrary
{
    private Dictionary<Guid, MovieEntry> _movies = new Dictionary<Guid, MovieEntry>();
    
    public void AddMovie(MovieEntry movie)
    {
        _movies[movie.MovieId] = movie;
    }
    public void RemoveMovie(Guid movieId)
    {
        _movies.Remove(movieId);
    }
    public bool TryGetMovie(Guid movieId, out MovieEntry? movie)
    {
        return _movies.TryGetValue(movieId, out movie);
    }
    public IEnumerable<MovieEntry> GetAllMovies()
    {
        return _movies.Values;
    }
}