namespace CinemaBookingApp.Domain.Money;

public class NormalizeMoney
{
    public decimal Convert(decimal amount)
    {
        return decimal.Round(amount, 2);
    }
}