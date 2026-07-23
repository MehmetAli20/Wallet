using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Wallet.Domain.Common;

namespace Wallet.UnitTests.Domain.Common
{
    public class MoneyTests
    {
        [Fact] 
        public void Add_WithSameCurrency_ReturnsSummedAmount() 
        {
            var value1 = new Money(10m, "USD");
            var value2 = new Money(5m, "USD");

            var result = value1.Add(value2);

            result.Amount.Should().Be(15m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Subtract_WithSameCurrency_ReturnsRemainingAmount()
        {
            var value1 = new Money(15m, "USD");
            var value2 = new Money(5m, "USD");

            var result = value1.Subtract(value2);

            result.Amount.Should().Be(10m);
            result.Currency.Should().Be("USD");
        }

        [Fact]
        public void Subtract_CanProduceNegativeAmount()
        {
            var value1 = new Money(5m, "USD");
            var value2 = new Money(15m, "USD");

            var result = value1.Subtract(value2);

            result.Amount.Should().Be(-10m);
            result.Currency.Should().Be("USD");
        }
        [Fact]
        public void Constructor_WithValidInput_SetsAmountAndCurrency()
        {
            var money = new Money(100m, "USD");

            money.Amount.Should().Be(100m);
            money.Currency.Should().Be("USD");
        }

        [Theory]
        [InlineData("")]      
        [InlineData("TR")]    
        [InlineData("TRYX")]
        [InlineData(null)]
        public void Constructor_WithInvalidCurrency_ThrowsArgumentException(string? currency)
        {
            var act = () => new Money(100m, currency!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TwoMoneys_WithSameAmountAndCurrency_AreEqual()
        {
            var a = new Money(50m, "EUR");
            var b = new Money(50m, "EUR");

            a.Should().Be(b);
        }

        [Fact]
        public void TwoMoneys_WithDifferentValues_AreNotEqual()
        {
            var a = new Money(50m, "EUR");
            var b = new Money(50m, "USD");

            a.Should().NotBe(b);
        }

        [Fact]
        public void Add_WithDifferentCurrency_ThrowsInvalidOperationException()
        {
            var tryMoney = new Money(10m, "TRY");
            var usdMoney = new Money(10m, "USD");

            var act = () => tryMoney.Add(usdMoney);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Subtract_WithDifferentCurrency_ThrowsInvalidOperationException()
        {
            var tryMoney = new Money(10m, "TRY");
            var usdMoney = new Money(5m, "USD");

            var act = () => tryMoney.Subtract(usdMoney);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Add_DoesNotMutateOperands()
        {
            var ten = new Money(10m, "TRY");
            var five = new Money(5m, "TRY");

            _ = ten.Add(five);

            ten.Amount.Should().Be(10m);
            five.Amount.Should().Be(5m);
        }
    }
}
