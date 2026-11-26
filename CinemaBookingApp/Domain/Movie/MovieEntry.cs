namespace CinemaBookingApp.Domain.Movie;

public sealed class MovieEntry(Guid movieId, string title, Genre genre, TimeSpan duration)
{
    public Guid MovieId { get; } = movieId;
    public string Name { get; } = title;
    public string Title { get; } = title;
    public Genre Genre { get; } = genre;
    public TimeSpan Duration { get; } = duration;
}