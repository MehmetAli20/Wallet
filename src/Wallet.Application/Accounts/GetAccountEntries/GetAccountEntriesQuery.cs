using MediatR;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Accounts.GetAccountEntries
{
    public record GetAccountEntriesQuery(Guid AccountId, int Skip, int Take)
        : IRequest<IReadOnlyList<LedgerEntry>>;
}
