using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class UserReportTests
    {
        private static UserReport NewReport(string? details = null) =>
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ReportReason.Spam, details, DateTimeOffset.UtcNow);

        [Fact]
        public void ReportingYourself_IsRejected()
        {
            var userId = Guid.NewGuid();

            var act = () => new UserReport(
                Guid.NewGuid(), userId, userId, ReportReason.Spam, null, DateTimeOffset.UtcNow);

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("  rude name  ", "rude name")]
        [InlineData("   ", null)]
        public void Details_AreTrimmed_AndBlankDetailsAreDropped(string details, string? expected) =>
            NewReport(details).Details.Should().Be(expected);

        [Fact]
        public void ResolvingTwice_KeepsTheFirstResolution()
        {
            var report = NewReport();
            var firstAdmin = Guid.NewGuid();
            var firstTime = DateTimeOffset.UtcNow;

            report.Resolve(firstAdmin, firstTime);
            report.Resolve(Guid.NewGuid(), firstTime.AddHours(1));

            report.ResolvedBy.Should().Be(firstAdmin);
            report.ResolvedAt.Should().Be(firstTime);
        }
    }
}
