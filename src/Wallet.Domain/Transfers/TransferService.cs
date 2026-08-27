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
        public void Settle(Account payer, Account payee, Money amount, Guid groupId)
        {
            if (payer == null)
            {
                throw new ArgumentNullException(nameof(payer));
            }

            if (payee == null)
            {
                throw new ArgumentNullException(nameof(payee));
            }

            if (amount == null)
            {
                throw new ArgumentNullException(nameof(amount));
            }

            if (amount.Amount <= 0)
            {
                throw new ArgumentException("Settlement amount must be greater than zero.", nameof(amount));
            }

            if (payer.Id == payee.Id)
            {
                throw new InvalidOperationException("Payer and payee accounts must be different.");
            }

            if (payer.Balance.Currency != payee.Balance.Currency || payee.Balance.Currency != amount.Currency)
            {
                throw new CurrencyMismatchException(payee.Balance.Currency, payer.Balance.Currency);
            }

            payer.Credit(amount, groupId, payee.OwnerId);
            payee.Debit(amount, groupId, payer.OwnerId);
        }
    }
}