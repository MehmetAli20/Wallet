using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Transfers;
using Wallet.Domain.Transfers;

namespace Wallet.Infrastructure.Persistence.Repositories.TransferRepository
{
    public class ScheduledTransferRepository : IScheduledTransferRepository
    {
        private readonly WalletDbContext _context;

        public ScheduledTransferRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ScheduledTransfer scheduledTransfer, CancellationToken cancellationToken)
        {
            await _context.ScheduledTransfers.AddAsync(scheduledTransfer, cancellationToken);
        }

        public async Task<IReadOnlyList<ScheduledTransfer>> GetDueAsync(
            DateTimeOffset asOf, CancellationToken cancellationToken = default)
        {
            return await _context.ScheduledTransfers
                .Where(x => x.Status == ScheduledTransferStatus.Pending && x.ScheduledFor <= asOf)
                .ToListAsync(cancellationToken);
        }
    }
}
