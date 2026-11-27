using CinemaBookingApp.Cli;

namespace CinemaBookingApp;

public static class Program
{
    public static void Main(string[] args) => new CinemaConsole(Console.In, Console.Out).Run();
}