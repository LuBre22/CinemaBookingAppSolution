namespace CinemaBookingApp.Domain.Money;

public static class NormalizeMoney
{
    public static decimal Convert(decimal amount)
    {
        return decimal.Round(amount, 2);
    }
}