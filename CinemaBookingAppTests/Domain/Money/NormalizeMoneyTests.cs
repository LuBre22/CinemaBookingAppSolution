using CinemaBookingApp.Domain.Money;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Money;

public class NormalizeMoneyTests
{
    [Fact]
    public void Convert_ShouldNormalizeIntegerAmountToTwoDecimalPlaces()
    {
        var sut = new NormalizeMoney();

        var amount = sut.Convert(12m);

        amount.Should().Be(12.00m);
        (decimal.GetBits(amount)[3] >> 16 & 0xFF).Should().Be(2);
    }
}
