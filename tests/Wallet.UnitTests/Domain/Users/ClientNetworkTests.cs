using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class ClientNetworkTests
    {
        [Theory]
        [InlineData("203.0.113.10", "203.0.113.10", true)]
        [InlineData("203.0.113.10", "203.0.113.11", false)]
        [InlineData("::ffff:203.0.113.10", "203.0.113.10", true)]
        [InlineData("2001:db8:1:2::10", "2001:db8:1:2:abcd::1", true)]
        [InlineData("2001:db8:1:2::10", "2001:db8:1:3::10", false)]
        public void Familiarity_ComparesNetworks_NotJustAddresses(string seen, string known, bool familiar) =>
            ClientNetwork.IsFamiliar(seen, [known]).Should().Be(familiar);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-an-ip")]
        public void AnUnknownSource_IsNeverFamiliar(string? seen) =>
            ClientNetwork.IsFamiliar(seen, ["203.0.113.10"]).Should().BeFalse();

        [Fact]
        public void WithoutAnySuccessfulLogin_NothingIsFamiliar() =>
            ClientNetwork.IsFamiliar("203.0.113.10", []).Should().BeFalse();
    }
}
