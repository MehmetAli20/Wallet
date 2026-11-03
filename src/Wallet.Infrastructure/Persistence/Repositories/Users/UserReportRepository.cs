using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Users;
using Wallet.Domain.Users;

namespace Wallet.Infrastructure.Persistence.Repositories.Users
{
    public class UserReportRepository : IUserReportRepository
    {
        private const int OpenReportLimit = 100;

        private readonly WalletDbContext _context;

        public UserReportRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasOpenReportAsync(
            Guid reporterId, Guid reportedUserId, CancellationToken cancellationToken = default) =>
            await _context.UserReports
                .AnyAsync(r => r.ReporterId == reporterId
                            && r.ReportedUserId == reportedUserId
                            && r.ResolvedAt == null, cancellationToken);

        public async Task<UserReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default) =>
            await _context.UserReports
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        public async Task<IReadOnlyList<OpenReport>> GetOpenAsync(CancellationToken cancellationToken = default) =>
            await _context.UserReports
                .IgnoreQueryFilters()
                .Where(r => r.ResolvedAt == null)
                .OrderBy(r => r.ReportedAt)
                .Take(OpenReportLimit)
                .Select(r => new OpenReport(
                    r.Id,
                    r.ReporterId,
                    _context.Users.Where(u => u.Id == r.ReporterId).Select(u => u.DisplayName).First(),
                    r.ReportedUserId,
                    _context.Users.Where(u => u.Id == r.ReportedUserId).Select(u => u.DisplayName).First(),
                    r.Reason,
                    r.Details,
                    r.ReportedAt))
                .ToListAsync(cancellationToken);

        public async Task AddAsync(UserReport report, CancellationToken cancellationToken = default) =>
            await _context.UserReports.AddAsync(report, cancellationToken);
    }
}
