using Wallet.Domain.Common;

namespace Wallet.Domain.Expenses
{
    public class ExpenseSplit
    {
        public Guid Id { get; private set; }
        public Guid ExpenseId { get; private set; }
        public Guid ParticipantId { get; private set; }
        public Money Share { get; private set; }

        private ExpenseSplit()
        {
            Share = null!;
        }

        internal ExpenseSplit(Guid id, Guid expenseId, Guid participantId, Money share)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Split Id cannot be empty.", nameof(id));

            if (expenseId == Guid.Empty)
                throw new ArgumentException("Expense Id cannot be empty.", nameof(expenseId));

            if (participantId == Guid.Empty)
                throw new ArgumentException("Participant Id cannot be empty.", nameof(participantId));

            Id = id;
            ExpenseId = expenseId;
            ParticipantId = participantId;
            Share = share;
        }
    }
}
