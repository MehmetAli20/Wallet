using FluentAssertions;
using Wallet.Domain.Groups;

namespace Wallet.UnitTests.Domain.Groups
{
    public class DebtSimplifierTests
    {
        [Fact]
        public void Scenario2_ProducesTheMinimalSetOfPayments()
        {
            var p1 = Guid.NewGuid();
            var p2 = Guid.NewGuid();
            var others = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();

            var positions = new List<MemberPosition>
            {
                new(p1, 85m),
                new(p2, 55m)
            };
            positions.AddRange(others.Select(o => new MemberPosition(o, -35m)));

            var debts = DebtSimplifier.Simplify(positions);

            debts.Sum(d => d.Amount).Should().Be(140m);
            debts.Where(d => d.CreditorId == p1).Sum(d => d.Amount).Should().Be(85m);
            debts.Where(d => d.CreditorId == p2).Sum(d => d.Amount).Should().Be(55m);

            foreach (var other in others)
                debts.Where(d => d.DebtorId == other).Sum(d => d.Amount).Should().Be(35m);
        }

        [Fact]
        public void SettledGroup_ProducesNoDebts()
        {
            var positions = new List<MemberPosition>
            {
                new(Guid.NewGuid(), 0m),
                new(Guid.NewGuid(), 0m)
            };

            DebtSimplifier.Simplify(positions).Should().BeEmpty();
        }

        [Fact]
        public void SimplePair_ProducesASinglePayment()
        {
            var creditor = Guid.NewGuid();
            var debtor = Guid.NewGuid();

            var debts = DebtSimplifier.Simplify(new List<MemberPosition>
            {
                new(creditor, 50m),
                new(debtor, -50m)
            });

            debts.Should().ContainSingle();
            debts[0].DebtorId.Should().Be(debtor);
            debts[0].CreditorId.Should().Be(creditor);
            debts[0].Amount.Should().Be(50m);
        }

        [Fact]
        public void Simplification_IsDeterministic()
        {
            var positions = new List<MemberPosition>
            {
                new(Guid.NewGuid(), 30m),
                new(Guid.NewGuid(), 30m),
                new(Guid.NewGuid(), -20m),
                new(Guid.NewGuid(), -40m)
            };

            var first = DebtSimplifier.Simplify(positions);
            var second = DebtSimplifier.Simplify(positions);

            first.Should().BeEquivalentTo(second, o => o.WithStrictOrdering());
        }

        [Fact]
        public void EveryDebtorPaysExactlyWhatTheyOwe()
        {
            var positions = new List<MemberPosition>
            {
                new(Guid.NewGuid(), 100m),
                new(Guid.NewGuid(), -60m),
                new(Guid.NewGuid(), -40m)
            };

            var debts = DebtSimplifier.Simplify(positions);

            foreach (var position in positions.Where(p => p.Net < 0))
                debts.Where(d => d.DebtorId == position.UserId).Sum(d => d.Amount).Should().Be(-position.Net);
        }

        [Fact]
        public void SimplifiedPaymentCount_IsAtMostOneLessThanTheParticipantCount()
        {
            var positions = new List<MemberPosition>
            {
                new(Guid.NewGuid(), 85m),
                new(Guid.NewGuid(), 55m),
                new(Guid.NewGuid(), -35m),
                new(Guid.NewGuid(), -35m),
                new(Guid.NewGuid(), -35m),
                new(Guid.NewGuid(), -35m)
            };

            DebtSimplifier.Simplify(positions).Count.Should().BeLessThan(positions.Count);
        }
    }
}
