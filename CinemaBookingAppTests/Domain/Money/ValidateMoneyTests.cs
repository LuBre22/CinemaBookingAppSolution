using CinemaBookingApp.Domain.Money;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Money;

public class ValidateMoneyTests
{
    [Fact]
    public void IsPositive_ShouldReturnFalseForZero()
    {
        var sut = new ValidateMoney();

        var result = sut.IsPositive(0m);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsPositive_ShouldReturnFalseForNegativeAmount()
    {
        var sut = new ValidateMoney();

        var result = sut.IsPositive(-1.25m);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTwoDecimals_ShouldReturnFalseForAmountsWithMoreThanTwoDecimalPlaces()
    {
        var sut = new ValidateMoney();

        var result = sut.IsTwoDecimals(12.345m);

        result.Should().BeFalse();
    }
}
