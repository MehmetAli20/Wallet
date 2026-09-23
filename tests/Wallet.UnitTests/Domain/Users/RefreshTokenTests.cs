using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class RefreshTokenTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        private static IssuedRefreshToken Start() =>
            RefreshToken.StartSession(Guid.NewGuid(), Guid.NewGuid(), T0);

        [Fact]
        public void StartSession_OpensAFamilyWithAnAbsoluteDeadline()
        {
            var issued = Start();

            issued.Token.Should().StartWith($"{RefreshToken.TokenPrefix}_");
            issued.RefreshToken.TokenHash.Should().NotBe(issued.Token);
            issued.RefreshToken.FamilyId.Should().NotBeEmpty();
            issued.RefreshToken.ExpiresAt.Should().Be(T0 + RefreshToken.Lifetime);
            issued.RefreshToken.SessionExpiresAt.Should().Be(T0 + RefreshToken.AbsoluteLifetime);
            issued.RefreshToken.IsActive(T0).Should().BeTrue();
        }

        [Fact]
        public void TwoSessions_NeverShareAFamily()
        {
            Start().RefreshToken.FamilyId.Should().NotBe(Start().RefreshToken.FamilyId);
        }

        [Fact]
        public void Successor_StaysInTheFamily_AndInheritsTheDeadline()
        {
            var first = Start().RefreshToken;
            var later = T0.AddDays(1);

            first.MarkUsed(later);
            var next = first.Successor(Guid.NewGuid(), later).RefreshToken;

            next.FamilyId.Should().Be(first.FamilyId);
            next.UserId.Should().Be(first.UserId);
            next.SessionExpiresAt.Should().Be(first.SessionExpiresAt);
            next.ExpiresAt.Should().Be(later + RefreshToken.Lifetime);
        }

        [Fact]
        public void Successor_NeverOutlivesTheSession()
        {
            var first = Start().RefreshToken;

            var day13 = T0.AddDays(13);
            first.MarkUsed(day13);
            var second = first.Successor(Guid.NewGuid(), day13).RefreshToken;

            var day26 = T0.AddDays(26);
            second.MarkUsed(day26);
            var third = second.Successor(Guid.NewGuid(), day26).RefreshToken;

            second.ExpiresAt.Should().Be(day13 + RefreshToken.Lifetime);
            third.ExpiresAt.Should().Be(first.SessionExpiresAt);
        }

        [Fact]
        public void PastTheAbsoluteDeadline_ThereIsNoSuccessor()
        {
            var first = Start().RefreshToken;

            var act = () => first.Successor(Guid.NewGuid(), first.SessionExpiresAt);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AnExpiredToken_IsNotActive()
        {
            var token = Start().RefreshToken;

            token.IsActive(token.ExpiresAt.AddTicks(-1)).Should().BeTrue();
            token.IsActive(token.ExpiresAt).Should().BeFalse();
        }

        [Fact]
        public void MarkUsed_Twice_Throws()
        {
            var token = Start().RefreshToken;
            token.MarkUsed(T0);

            var act = () => token.MarkUsed(T0.AddSeconds(1));

            act.Should().Throw<InvalidOperationException>();
            token.IsActive(T0.AddSeconds(1)).Should().BeFalse();
        }

        [Fact]
        public void AUsedToken_IsToleratedOnlyForTheReuseInterval()
        {
            var token = Start().RefreshToken;
            token.MarkUsed(T0);

            token.IsWithinReuseInterval(T0 + RefreshToken.ReuseInterval).Should().BeTrue();
            token.IsWithinReuseInterval(T0 + RefreshToken.ReuseInterval + TimeSpan.FromMilliseconds(1))
                .Should().BeFalse();
        }

        [Fact]
        public void AnUnusedToken_IsNeverWithinTheReuseInterval()
        {
            Start().RefreshToken.IsWithinReuseInterval(T0).Should().BeFalse();
        }

        [Fact]
        public void Revoke_DeactivatesTheToken_AndIsIdempotent()
        {
            var token = Start().RefreshToken;

            token.Revoke(T0);
            token.Revoke(T0.AddMinutes(1));

            token.RevokedAt.Should().Be(T0);
            token.IsActive(T0).Should().BeFalse();
        }

        [Fact]
        public void ARevokedToken_HasNoGrace_AndNoSuccessor()
        {
            var token = Start().RefreshToken;
            token.MarkUsed(T0);
            token.Revoke(T0);

            token.IsWithinReuseInterval(T0).Should().BeFalse();

            var act = () => token.Successor(Guid.NewGuid(), T0);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
