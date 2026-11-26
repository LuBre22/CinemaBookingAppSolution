using CinemaBookingApp.Domain.Money;
using FluentAssertions;

namespace CinemaBookingAppTests.Domain.Money;

public class NormalizeMoneyTests
{
    [Theory]
    [InlineData("12", "12.00")]
    [InlineData("12.0", "12.00")]
    [InlineData("12.5", "12.50")]
    [InlineData("12.50", "12.50")]
    [InlineData("0", "0.00")]
    [InlineData("-12", "-12.00")]
    public void AddTrailingZeroes_ShouldPreserveValueAndProduceScaleTwo(string input, string expected)
    {
        var amount = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

        var result = NormalizeMoney.AddTrailingZeroes(amount);

        result.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
        ((decimal.GetBits(result)[3] >> 16) & 0xFF).Should().Be(2);
    }

    [Theory]
    [InlineData("12.345")]
    [InlineData("12.500")]
    public void AddTrailingZeroes_ShouldRejectMoreThanTwoDecimalPlaces(string input)
    {
        var amount = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        Action act = () => NormalizeMoney.AddTrailingZeroes(amount);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("amount");
    }

    [Fact]
    public void AddTrailingZeroes_ShouldRejectValuesThatCannotHaveScaleTwo()
    {
        Action act = () => NormalizeMoney.AddTrailingZeroes(decimal.MaxValue);

        act.Should().Throw<OverflowException>();
    }
}
