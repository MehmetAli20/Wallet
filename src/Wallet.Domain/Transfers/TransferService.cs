using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Transfers
{
    public class TransferService
    {
        public void Transfer(Account source, Account destination, Money amount, Guid groupId)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (amount == null)
            {
                throw new ArgumentNullException(nameof(amount));
            }
            
            if(amount.Amount <= 0)
            {
                throw new ArgumentException("Transfer amount must be greater than zero.", nameof(amount));
            }

            if (source.Id == destination.Id)
            {
                throw new InvalidOperationException("Source and destination accounts must be different.");
            }

            if (source.Balance.Currency != destination.Balance.Currency || destination.Balance.Currency != amount.Currency)
            {
                throw new CurrencyMismatchException(destination.Balance.Currency, source.Balance.Currency);
            }

            source.Debit(amount, groupId);
            destination.Credit(amount, groupId);
        }
    }
}