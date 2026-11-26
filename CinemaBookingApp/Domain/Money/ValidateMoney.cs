namespace CinemaBookingApp.Domain.Money;

public class ValidateMoney
{
    public bool IsPositive(decimal amount)
    {
        return amount > 0;
    }

    public bool IsTwoDecimals(decimal amount)
    {
        return decimal.Round(amount, 2) == amount;
    }
}