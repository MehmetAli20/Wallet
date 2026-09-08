using System;

namespace Wallet.Domain.Users
{
    public class LoginAttempt
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public DateTimeOffset AttemptedAt { get; private set; }
        public bool Succeeded { get; private set; }
        public string? ClientIp { get; private set; }
        public DateTimeOffset? LockedUntil { get; private set; }

        private LoginAttempt()
        {
        }

        private LoginAttempt(
            Guid id,
            Guid userId,
            DateTimeOffset attemptedAt,
            bool succeeded,
            string? clientIp,
            DateTimeOffset? lockedUntil)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Login attempt Id cannot be empty.", nameof(id));
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User Id cannot be empty.", nameof(userId));
            }

            Id = id;
            UserId = userId;
            AttemptedAt = attemptedAt.ToUniversalTime();
            Succeeded = succeeded;
            ClientIp = clientIp;
            LockedUntil = lockedUntil?.ToUniversalTime();
        }

        public static LoginAttempt Success(Guid id, Guid userId, DateTimeOffset attemptedAt, string? clientIp)
        {
            return new LoginAttempt(id, userId, attemptedAt, succeeded: true, clientIp, lockedUntil: null);
        }

        public static LoginAttempt Failure(
            Guid id, Guid userId, DateTimeOffset attemptedAt, string? clientIp, DateTimeOffset? lockedUntil)
        {
            return new LoginAttempt(id, userId, attemptedAt, succeeded: false, clientIp, lockedUntil);
        }
    }
}
