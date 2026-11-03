using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Abstractions.Users
{
    public interface IUserReportRepository
    {
        Task<bool> HasOpenReportAsync(Guid reporterId, Guid reportedUserId, CancellationToken cancellationToken = default);
        Task<UserReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OpenReport>> GetOpenAsync(CancellationToken cancellationToken = default);
        Task AddAsync(UserReport report, CancellationToken cancellationToken = default);
    }
}
