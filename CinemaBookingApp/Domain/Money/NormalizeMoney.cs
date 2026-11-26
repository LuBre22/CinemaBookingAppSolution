namespace CinemaBookingApp.Domain.Money;

public static class NormalizeMoney
{
    public static decimal AddTrailingZeroes(decimal amount)
    {
        var scale = (decimal.GetBits(amount)[3] >> 16) & 0xFF;
        var normalized = scale switch
        {
            0 => amount * 1.00m,
            1 => amount * 1.0m,
            2 => amount,
            _ => throw new ArgumentOutOfRangeException(nameof(amount), "Amount must have at most two decimal places.")
        };

        if (((decimal.GetBits(normalized)[3] >> 16) & 0xFF) != 2)
            throw new OverflowException("Amount cannot be represented with two decimal places.");

        return normalized;
    }
}