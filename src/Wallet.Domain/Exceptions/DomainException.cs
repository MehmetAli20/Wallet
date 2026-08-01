using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
    }

    public class AccountNotFoundException : DomainException
    {
        public AccountNotFoundException(Guid accountId) : base ($"Account {accountId} was not found.") { }
    }

    public class InsufficientFundsException : DomainException
    {
        public InsufficientFundsException(Guid accountId)
            : base($"Account {accountId} has insufficient funds for this operation.") { }
    }

    public class CurrencyMismatchException : DomainException
    {
        public CurrencyMismatchException(string expected, string actual)
            : base($"Currency mismatch: expected {expected}, got {actual}.") { }
        // Transfer ederkenfarkli para birimi secilmesini engellemeliyim ileride,
        // source.Currency => destination.Currency ayniysa kullanici transfer esnasinda baska para birimi secememeli
    }

    public class InvalidTransferException : DomainException
    {
        public InvalidTransferException(string message) : base(message) { }
    }
}