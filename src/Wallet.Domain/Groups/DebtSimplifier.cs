namespace Wallet.Domain.Groups
{
    public static class DebtSimplifier
    {
        public static IReadOnlyList<PairwiseDebt> Simplify(IReadOnlyList<MemberPosition> positions)
        {
            var creditors = positions
                .Where(p => p.Net > 0)
                .OrderByDescending(p => p.Net).ThenBy(p => p.UserId)
                .Select(p => (p.UserId, Remaining: p.Net))
                .ToList();

            var debtors = positions
                .Where(p => p.Net < 0)
                .OrderBy(p => p.Net).ThenBy(p => p.UserId)
                .Select(p => (p.UserId, Remaining: -p.Net))
                .ToList();

            var debts = new List<PairwiseDebt>();
            var c = 0;
            var d = 0;

            while (c < creditors.Count && d < debtors.Count)
            {
                var amount = Math.Min(creditors[c].Remaining, debtors[d].Remaining);

                if (amount > 0)
                    debts.Add(new PairwiseDebt(debtors[d].UserId, creditors[c].UserId, amount));

                creditors[c] = (creditors[c].UserId, creditors[c].Remaining - amount);
                debtors[d] = (debtors[d].UserId, debtors[d].Remaining - amount);

                if (creditors[c].Remaining == 0) c++;
                if (debtors[d].Remaining == 0) d++;
            }

            return debts;
        }
    }
}
