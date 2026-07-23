using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Domain.Common
{
    public sealed record Money
    {
        public decimal Amount { get; }
        public string Currency { get; }
        public Money(decimal amount,string currency)
        {
            if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsLetter))
                throw new ArgumentException("Currency must be a 3-letter code.", nameof(currency));
            
            Amount = amount;
            Currency = currency.ToUpperInvariant();
        }
        public Money Add(Money other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            EnsureSameCurrency(other);

            return new Money(Amount +  other.Amount, Currency);
        }
        public Money Subtract(Money other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            EnsureSameCurrency(other);
            
            return new Money(Amount-other.Amount, Currency);
        }

        private void EnsureSameCurrency(Money other)
        {
            if (Currency != other.Currency)
            {
                throw new InvalidOperationException("Currencies must match.");
            }
        }
    }
}
