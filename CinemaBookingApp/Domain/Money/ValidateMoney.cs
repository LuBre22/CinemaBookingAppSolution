namespace CinemaBookingApp.Domain.Money;

public static class ValidateMoney
{
    public static bool IsPositive(decimal amount)
    {
        return amount > 0;
    }

    public static bool IsTwoDecimals(decimal amount)
    {
        return ((decimal.GetBits(amount)[3] >> 16) & 0xFF) <= 2;
    }
}