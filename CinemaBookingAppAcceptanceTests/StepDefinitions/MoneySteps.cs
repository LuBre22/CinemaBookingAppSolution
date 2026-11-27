using System.Globalization;
using CinemaBookingApp.Domain.Money;
using FluentAssertions;
using Reqnroll;

namespace CinemaBookingAppAcceptanceTests.StepDefinitions;

[Binding]
[Scope(Feature = "Ticket prices")]
public sealed class MoneySteps
{
    private decimal? _amount;
    private bool _rejected;

    [When(@"a money amount of ([-\d.]+) EUR is created")]
    public void CreateAmount(string value)
    {
        var amount = decimal.Parse(value, CultureInfo.InvariantCulture);
        _rejected = !ValidateMoney.IsPositive(amount) || !ValidateMoney.IsTwoDecimals(amount);
        if (!_rejected)
            _amount = NormalizeMoney.AddTrailingZeroes(amount);
    }

    [Then(@"the amount is represented as ([-\d.]+) EUR")]
    public void AmountIsRepresented(string value)
    {
        _rejected.Should().BeFalse();
        _amount.Should().NotBeNull();
        _amount!.Value.ToString(CultureInfo.InvariantCulture).Should().Be(value);
        ((decimal.GetBits(_amount.Value)[3] >> 16) & 0xFF).Should().Be(2);
    }

    [Then("the amount is rejected as invalid")]
    public void AmountIsRejected()
    {
        _rejected.Should().BeTrue();
        _amount.Should().BeNull();
    }
}
