namespace CinemaBookingAppTests;

internal static class TestSeatIds
{
    public static string[] Create(int count) =>
        Enumerable.Range(1, count).Select(number => $"Seat-{number}").ToArray();
}
