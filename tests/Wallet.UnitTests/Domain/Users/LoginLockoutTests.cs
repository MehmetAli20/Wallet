using FluentAssertions;
using Wallet.Domain.Users;

namespace Wallet.UnitTests.Domain.Users
{
    public class LoginLockoutTests
    {
        private static readonly Guid UserId = Guid.NewGuid();
        private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        private static LoginAttempt Failure(DateTimeOffset at, DateTimeOffset? lockedUntil = null) =>
            LoginAttempt.Failure(Guid.NewGuid(), UserId, at, clientIp: null, lockedUntil);

        private static LoginAttempt Success(DateTimeOffset at) =>
            LoginAttempt.Success(Guid.NewGuid(), UserId, at, clientIp: null);

        [Fact]
        public void WithNoHistory_TheAccountIsOpen()
        {
            var history = Array.Empty<LoginAttempt>();

            LoginLockout.IsLockedOut(history, Now).Should().BeFalse();
            LoginLockout.LockoutForNextFailure(history, Now).Should().BeNull();
        }

        [Fact]
        public void TheFailureThatReachesTheThreshold_CarriesALockout()
        {
            var history = new List<LoginAttempt>();

            for (var i = 1; i < LoginLockout.MaxFailedAttempts; i++)
            {
                LoginLockout.LockoutForNextFailure(history, Now)
                    .Should().BeNull($"failure {i} is below the threshold");

                history.Insert(0, Failure(Now));
            }

            LoginLockout.LockoutForNextFailure(history, Now)
                .Should().Be(Now.Add(LoginLockout.Duration));
        }

        [Fact]
        public void WhileTheLockoutStands_TheAccountIsLocked()
        {
            var history = new[] { Failure(Now, Now.Add(LoginLockout.Duration)) };

            LoginLockout.IsLockedOut(history, Now.AddMinutes(14)).Should().BeTrue();
        }

        [Fact]
        public void OnceTheLockoutPasses_TheAccountIsOpenAgain()
        {
            var history = new[] { Failure(Now, Now.Add(LoginLockout.Duration)) };

            LoginLockout.IsLockedOut(history, Now.AddMinutes(16)).Should().BeFalse();
        }

        [Fact]
        public void AfterALockoutExpires_ItTakesAFullSetOfFailuresToLockAgain()
        {
            var lockedAt = Now.AddMinutes(-20);
            var history = new List<LoginAttempt> { Failure(lockedAt, lockedAt.Add(LoginLockout.Duration)) };

            for (var i = 1; i < LoginLockout.MaxFailedAttempts; i++)
            {
                LoginLockout.LockoutForNextFailure(history, Now)
                    .Should().BeNull($"failure {i} after the lockout is below the threshold again");

                history.Insert(0, Failure(Now));
            }

            LoginLockout.LockoutForNextFailure(history, Now).Should().NotBeNull();
        }

        [Fact]
        public void ASuccessAmongTheRecentAttempts_StartsTheCountOver()
        {
            var history = new[]
            {
                Failure(Now),
                Failure(Now),
                Success(Now.AddMinutes(-1)),
                Failure(Now.AddMinutes(-2)),
                Failure(Now.AddMinutes(-3))
            };

            LoginLockout.LockoutForNextFailure(history, Now).Should().BeNull();
        }

        [Fact]
        public void ALockoutAnywhereInTheHistory_CountsEvenIfItIsNotTheNewestRow()
        {
            var history = new[]
            {
                Failure(Now),
                Failure(Now, Now.Add(LoginLockout.Duration))
            };

            LoginLockout.IsLockedOut(history, Now).Should().BeTrue();
        }
    }
}
