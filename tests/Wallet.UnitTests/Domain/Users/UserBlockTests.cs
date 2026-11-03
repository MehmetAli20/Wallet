using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class UserBlockTests
    {
        [Fact]
        public void BlockingYourself_IsRejected()
        {
            var userId = Guid.NewGuid();

            var act = () => new UserBlock(userId, userId, DateTimeOffset.UtcNow);

            act.Should().Throw<ArgumentException>();
        }
    }
}
