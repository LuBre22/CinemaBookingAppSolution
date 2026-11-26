using CinemaBookingApp.Domain.Money;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Money;

public class ValidateMoneyTests
{
    [Fact]
    public void IsPositive_ShouldReturnFalseForZero()
    {
        var result = ValidateMoney.IsPositive(0m);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsPositive_ShouldReturnFalseForNegativeAmount()
    {
        var result = ValidateMoney.IsPositive(-1.25m);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTwoDecimals_ShouldReturnFalseForAmountsWithMoreThanTwoDecimalPlaces()
    {
        var result = ValidateMoney.IsTwoDecimals(12.345m);

        result.Should().BeFalse();
    }
}
