using Wallet.Domain.Exceptions;

namespace Wallet.Domain.Expenses
{
    public static class ShareAllocator
    {
        private const decimal Cent = 0.01m;

        public static IReadOnlyList<(Guid ParticipantId, decimal Amount)> Allocate(
            decimal total,
            IReadOnlyList<Guid> participants,
            IReadOnlyDictionary<Guid, decimal> fixedShares)
        {
            if (total <= 0)
                throw new InvalidExpenseException("Expense total must be positive.");

            if (participants.Count == 0)
                throw new InvalidExpenseException("An expense needs at least one participant.");

            if (participants.Distinct().Count() != participants.Count)
                throw new InvalidExpenseException("A participant cannot appear twice.");

            if (fixedShares.Keys.Any(k => !participants.Contains(k)))
                throw new InvalidExpenseException("A fixed share was given for a non-participant.");

            if (fixedShares.Values.Any(v => v < 0))
                throw new InvalidExpenseException("A fixed share cannot be negative.");

            var fixedTotal = fixedShares.Values.Sum();

            if (fixedTotal > total)
                throw new InvalidExpenseException("Fixed shares exceed the expense total.");

            var free = participants.Where(p => !fixedShares.ContainsKey(p)).OrderBy(p => p).ToList();
            var remainder = total - fixedTotal;

            if (free.Count == 0 && remainder != 0m)
                throw new InvalidExpenseException("Fixed shares do not add up to the expense total.");

            var allocation = new Dictionary<Guid, decimal>(fixedShares);

            if (free.Count > 0)
            {
                var baseShare = Math.Floor(remainder / free.Count / Cent) * Cent;
                var leftoverCents = (int)((remainder - baseShare * free.Count) / Cent);

                for (var i = 0; i < free.Count; i++)
                    allocation[free[i]] = baseShare + (i < leftoverCents ? Cent : 0m);
            }

            var result = participants.Select(p => (ParticipantId: p, Amount: allocation[p])).ToList();

            if (result.Sum(x => x.Amount) != total)
                throw new InvalidExpenseException("Allocated shares do not sum to the expense total.");

            return result;
        }
    }
}
