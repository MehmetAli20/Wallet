using System;
using System.Collections.Generic;
using System.Linq;

namespace Wallet.Domain.Users
{
    public static class LoginLockout
    {
        public const int MaxFailedAttempts = 5;

        public static readonly TimeSpan Duration = TimeSpan.FromMinutes(15);

        public static bool IsLockedOut(IReadOnlyList<LoginAttempt> mostRecentFirst, DateTimeOffset now)
        {
            return mostRecentFirst.Any(attempt => attempt.LockedUntil > now.ToUniversalTime());
        }

        public static DateTimeOffset? LockoutForNextFailure(
            IReadOnlyList<LoginAttempt> mostRecentFirst, DateTimeOffset now)
        {
            var priorFailures = mostRecentFirst
                .TakeWhile(attempt => !attempt.Succeeded && attempt.LockedUntil is null)
                .Count();

            return priorFailures + 1 >= MaxFailedAttempts
                ? now.ToUniversalTime().Add(Duration)
                : null;
        }
    }
}
