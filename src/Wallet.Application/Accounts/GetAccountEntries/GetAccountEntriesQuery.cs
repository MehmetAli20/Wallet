using MediatR;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Accounts;
using Wallet.Domain.Exceptions;

namespace Wallet.Application.Accounts.GetAccountEntries
{
    public record GetAccountEntriesQuery(Guid AccountId, int Skip, int Take)
        : IRequest<IReadOnlyList<LedgerEntry>>;

    public class GetAccountEntriesQueryHandler
        : IRequestHandler<GetAccountEntriesQuery, IReadOnlyList<LedgerEntry>>
    {
        private readonly IAccountRepository _accounts;

        public GetAccountEntriesQueryHandler(IAccountRepository accounts)
        {
            _accounts = accounts;
        }

        public async Task<IReadOnlyList<LedgerEntry>> Handle(
            GetAccountEntriesQuery request, CancellationToken cancellationToken)
        {
            _ = await _accounts.GetByIdAsync(request.AccountId, cancellationToken)
                ?? throw new AccountNotFoundException(request.AccountId);

            return await _accounts.GetEntriesAsync(
                request.AccountId, request.Skip, request.Take, cancellationToken);
        }
    }
}
