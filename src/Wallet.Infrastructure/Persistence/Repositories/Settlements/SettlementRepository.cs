using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Settlements;
using Wallet.Domain.Settlements;

namespace Wallet.Infrastructure.Persistence.Repositories.Settlements
{
    public class SettlementRepository : ISettlementRepository
    {
        private readonly WalletDbContext _context;

        public SettlementRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default) =>
            await _context.Settlements.AddAsync(settlement, cancellationToken);

        public async Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _context.Settlements.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
