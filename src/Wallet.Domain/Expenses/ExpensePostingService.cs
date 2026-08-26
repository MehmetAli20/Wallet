using Wallet.Domain.Accounts;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Expenses
{
    public class ExpensePostingService
    {
        public void Post(Expense expense, IReadOnlyDictionary<Guid, Account> accounts)
        {
            if (expense is null)
                throw new ArgumentNullException(nameof(expense));

            if (accounts is null)
                throw new ArgumentNullException(nameof(accounts));

            if (!accounts.TryGetValue(expense.PayerId, out var payerAccount))
                throw new InvalidExpenseException("The payer has no account for this currency.");

            foreach (var split in expense.Splits)
            {
                if (!accounts.TryGetValue(split.ParticipantId, out var participantAccount))
                    throw new InvalidExpenseException("A participant has no account for this currency.");

                if (participantAccount.Currency != expense.Total.Currency)
                    throw new CurrencyMismatchException(expense.Total.Currency, participantAccount.Currency);
            }

            foreach (var split in expense.Splits)
            {
                if (split.Share.Amount <= 0 || split.ParticipantId == expense.PayerId)
                    continue;

                accounts[split.ParticipantId].Debit(split.Share, expense.GroupId, expense.PayerId);
                payerAccount.Credit(split.Share, expense.GroupId, split.ParticipantId);
            }
        }
    }
}
