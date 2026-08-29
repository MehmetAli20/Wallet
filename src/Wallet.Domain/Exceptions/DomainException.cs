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

    public class CurrencyMismatchException : DomainException
    {
        public CurrencyMismatchException(string expected, string actual)
            : base($"Currency mismatch: expected {expected}, got {actual}.") { }
    }

    public class InvalidTransferException : DomainException
    {
        public InvalidTransferException(string message) : base(message) { }
    }
    public class InvalidGroupOperationException : DomainException
    {
        public InvalidGroupOperationException(string message) : base(message) { }
    }
    public class InvalidExpenseException : DomainException
    {
        public InvalidExpenseException(string message) : base(message) { }
    }

    public class InvitationNotFoundException : DomainException
    {
        public InvitationNotFoundException(Guid invitationId)
            : base($"Invitation {invitationId} was not found.") { }
    }

    public class GroupNotFoundException : DomainException
    {
        public GroupNotFoundException(Guid groupId) : base($"Group {groupId} was not found.") { }
    }

    public class ExpenseNotFoundException : DomainException
    {
        public ExpenseNotFoundException(Guid expenseId) : base($"Expense {expenseId} was not found.") { }
    }
}