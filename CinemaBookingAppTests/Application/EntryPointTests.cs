namespace CinemaBookingAppTests.Application;

public class EntryPointTests
{
    [Fact]
    public void Main_ShouldReturnForEmptyArguments()
    {
        CinemaBookingApp.Program.Main(Array.Empty<string>());
    }
}
