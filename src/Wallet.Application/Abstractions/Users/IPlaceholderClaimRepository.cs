using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IPlaceholderClaimRepository
    {
        Task AddAsync(PlaceholderClaim claim, CancellationToken cancellationToken = default);

        Task<PlaceholderClaim?> GetUsableAsync(
            string token, DateTimeOffset asOf, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PlaceholderClaim>> GetOutstandingForPlaceholderAsync(
            Guid placeholderUserId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    }
}
