using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface ILoginAttemptRepository
    {
        Task<IReadOnlyList<LoginAttempt>> GetMostRecentAsync(
            Guid userId, int take, CancellationToken cancellationToken = default);

        Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default);

        Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    }
}
