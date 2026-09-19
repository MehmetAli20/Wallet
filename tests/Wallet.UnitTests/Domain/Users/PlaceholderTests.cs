using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class PlaceholderTests
    {
        [Fact]
        public void APlaceholder_HasANameButNoWayToSignIn()
        {
            var placeholder = User.CreatePlaceholder(Guid.NewGuid(), "  Mehmet  ");

            placeholder.IsPlaceholder.Should().BeTrue();
            placeholder.DisplayName.Should().Be("Mehmet");
            placeholder.Username.Should().BeNull();
            placeholder.Email.Should().BeNull();
            placeholder.PasswordHash.Should().BeNull();
        }

        [Fact]
        public void APlaceholderWithoutAName_IsRejected()
        {
            var act = () => User.CreatePlaceholder(Guid.NewGuid(), "   ");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Promote_TurnsThePlaceholderIntoARealUser_WithoutChangingItsId()
        {
            var id = Guid.NewGuid();
            var placeholder = User.CreatePlaceholder(id, "Mehmet");

            placeholder.Promote("mehmet_k", "Mehmet@Example.COM", "hash");

            placeholder.Id.Should().Be(id);
            placeholder.IsPlaceholder.Should().BeFalse();
            placeholder.Username.Should().Be("mehmet_k");
            placeholder.Email.Should().Be("mehmet@example.com");
            placeholder.PasswordHash.Should().Be("hash");
            placeholder.DisplayName.Should().Be("Mehmet");
        }

        [Fact]
        public void PromotingARealUser_Throws()
        {
            var user = new User(Guid.NewGuid(), "ali", "ali@test.com", "hash", UserRole.User);

            var act = () => user.Promote("ali2", "ali2@test.com", "hash2");

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ARegisteredUser_FallsBackToItsUsernameAsDisplayName()
        {
            var user = new User(Guid.NewGuid(), "Ali", "ali@test.com", "hash", UserRole.User);

            user.Username.Should().Be("ali");
            user.DisplayName.Should().Be("Ali");
        }

        [Fact]
        public void AClaimCanBeUsedOnce()
        {
            var issued = PlaceholderClaim.Issue(
                Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

            issued.Token.Should().StartWith($"{PlaceholderClaim.TokenPrefix}_");
            issued.Claim.TokenHash.Should().NotBe(issued.Token);
            issued.Claim.UsedAt.Should().BeNull();

            issued.Claim.Use(DateTimeOffset.UtcNow);

            issued.Claim.UsedAt.Should().NotBeNull();

            var act = () => issued.Claim.Use(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AnExpiredClaimCannotBeUsed()
        {
            var issued = PlaceholderClaim.Issue(
                Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1));

            var act = () => issued.Claim.Use(DateTimeOffset.UtcNow);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TwoClaims_NeverShareAToken()
        {
            var first = PlaceholderClaim.Issue(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));
            var second = PlaceholderClaim.Issue(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

            second.Token.Should().NotBe(first.Token);
        }
    }
}
