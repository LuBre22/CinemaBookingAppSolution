namespace CinemaBookingApp.Domain.Films;

public sealed class Film
{
    private readonly Guid _movieId;
    private readonly string _title;
    private readonly Genre _genre;
    private readonly TimeSpan _duration;
    
    public Film(Guid movieId, string title, Genre genre, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (!Enum.IsDefined(genre))
            throw new ArgumentOutOfRangeException(nameof(genre));

        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));
        
        _movieId = movieId;
        _title = title;
        _genre = genre;
        _duration = duration;
    }
    
    public Guid MovieId => _movieId;
    public string Title => _title;
    public Genre Genre => _genre;
    public TimeSpan Duration => _duration;
}