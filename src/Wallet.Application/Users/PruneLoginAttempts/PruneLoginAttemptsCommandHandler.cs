using MediatR;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.PruneLoginAttempts
{
    public class PruneLoginAttemptsCommandHandler : IRequestHandler<PruneLoginAttemptsCommand, int>
    {
        private readonly ILoginAttemptRepository _loginAttempts;

        public PruneLoginAttemptsCommandHandler(ILoginAttemptRepository loginAttempts)
        {
            _loginAttempts = loginAttempts;
        }

        public Task<int> Handle(PruneLoginAttemptsCommand request, CancellationToken cancellationToken)
        {
            var retention = TimeSpan.FromDays(request.RetentionDays);

            if (retention <= LoginLockout.Duration)
            {
                throw new ArgumentException(
                    $"Retention of {retention} would delete login attempts that a standing lockout " +
                    $"still depends on. It must be well above {LoginLockout.Duration}.",
                    nameof(request));
            }

            return _loginAttempts.DeleteOlderThanAsync(DateTimeOffset.UtcNow - retention, cancellationToken);
        }
    }
}
