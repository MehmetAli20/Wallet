using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Expenses
{
    public class ExpensePostingService
    {
        private readonly record struct Leg(Account Participant, Account Payer, Money Share);

        public void Post(Expense expense, IReadOnlyDictionary<Guid, Account> accounts)
        {
            foreach (var leg in Legs(expense, accounts))
            {
                leg.Participant.Debit(leg.Share, expense.GroupId, expense.PayerId);
                leg.Payer.Credit(leg.Share, expense.GroupId, leg.Participant.OwnerId);
            }
        }

        public void Reverse(Expense expense, IReadOnlyDictionary<Guid, Account> accounts)
        {
            foreach (var leg in Legs(expense, accounts))
            {
                leg.Participant.Credit(leg.Share, expense.GroupId, expense.PayerId);
                leg.Payer.Debit(leg.Share, expense.GroupId, leg.Participant.OwnerId);
            }
        }

        private static IReadOnlyList<Leg> Legs(Expense expense, IReadOnlyDictionary<Guid, Account> accounts)
        {
            if (expense is null)
                throw new ArgumentNullException(nameof(expense));

            if (accounts is null)
                throw new ArgumentNullException(nameof(accounts));

            if (!accounts.TryGetValue(expense.PayerId, out var payerAccount))
                throw new InvalidExpenseException("The payer has no account for this currency.");

            var legs = new List<Leg>();

            foreach (var split in expense.Splits)
            {
                if (!accounts.TryGetValue(split.ParticipantId, out var participantAccount))
                    throw new InvalidExpenseException("A participant has no account for this currency.");

                if (participantAccount.Currency != expense.Total.Currency)
                    throw new CurrencyMismatchException(expense.Total.Currency, participantAccount.Currency);

                if (split.Share.Amount <= 0 || split.ParticipantId == expense.PayerId)
                    continue;

                legs.Add(new Leg(participantAccount, payerAccount, split.Share));
            }

            return legs;
        }
    }
}
