using CinemaBookingApp.Cli;
using FluentAssertions;

namespace CinemaBookingAppTests.Cli;

public class CinemaConsoleTests
{
    [Fact]
    public void CustomerCanPayFailAndCancelWithBothAuthenticationOutcomes()
    {
        var output = Run(
            "2", "", "2", "1", "A1", "3", "1", "fail",
            "2", "1", "A1", "3", "1", "pass",
            "4", "5", "2", "fail", "5", "2", "pass", "4", "0", "0");

        output.Should().Contain("Payment declined. Booking DENIED.");
        output.Should().Contain("Payment approved. Booking CONFIRMED.");
        output.Should().Contain("Cancellation failed: The customer is not authorized");
        output.Should().Contain("Booking CANCELLED. Refund approved.");
        output.Should().Contain("DENIED").And.Contain("CANCELLED");
    }

    [Fact]
    public void ManagerCanCreateFilmScheduleAndCancelShowing()
    {
        var start = DateTime.Today.AddDays(3).AddHours(12).ToString("yyyy-MM-dd HH:mm");
        var output = Run(
            "1", "1", "Test film", "Drama", "100", "2", "2", "1", start, "12.50",
            "3", "4", "2", "3", "0", "0");

        output.Should().Contain("Film created: Test film");
        output.Should().Contain("Showing scheduled:");
        output.Should().Contain("Showing cancelled. Associated bookings were cancelled and refunded.");
        output.Should().Contain("Test film");
    }

    [Fact]
    public void EndOfInputExitsAndInvalidChoicesAreReported()
    {
        var output = Run("invalid");
        output.Should().Contain("Invalid choice.").And.Contain("Goodbye.");
    }

    [Fact]
    public void CartConflictsAreReportedWithoutReservingTwice()
    {
        var output = Run(
            "2", "", "2", "1", "A1", "2", "1", "A1", "3", "1", "pass",
            "3", "1", "pass", "4", "0", "0");

        output.Should().Contain("Payment approved. Booking CONFIRMED.");
        output.Should().Contain("Could not commit booking:").And.Contain("Status: DENIED.");
        output.Should().Contain("CONFIRMED").And.Contain("DENIED");
    }

    [Fact]
    public void ManagerCancellationUpdatesCartAndBookingHistoryAcrossModeSwitches()
    {
        var customer = Guid.NewGuid().ToString();
        var output = Run(
            "2", customer, "2", "1", "A1", "0",
            "1", "4", "1", "0",
            "2", customer, "3", "4", "0", "0");

        output.Should().Contain("Showing cancelled. Associated bookings were cancelled and refunded.");
        output.Should().Contain("Your cart is empty.");
        output.Should().Contain("CANCELLED");
    }

    [Fact]
    public void InvalidInputAndUnavailableSeatDoNotCreateBooking()
    {
        var output = Run(
            "2", "bad-id", "", "2", "9", "2", "1", "Z99", "4", "0", "0");

        output.Should().Contain("Enter a non-empty customer GUID");
        output.Should().Contain("Invalid choice.");
        output.Should().Contain("Booking rejected: InvalidOrUnavailableSeat.");
        output.Should().Contain("You have no bookings.");
    }

    private static string Run(params string[] lines)
    {
        using var input = new StringReader(string.Join(Environment.NewLine, lines) + Environment.NewLine);
        using var output = new StringWriter();
        new CinemaConsole(input, output).Run();
        return output.ToString();
    }
}
