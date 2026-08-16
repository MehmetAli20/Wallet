using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Accounts.GetMyAccounts
{
    public class GetMyAccountsQueryHandler : IRequestHandler<GetMyAccountsQuery, IReadOnlyList<Account>>
    {
        private readonly IAccountRepository _accounts;

        public GetMyAccountsQueryHandler(IAccountRepository accounts)
        {
            _accounts = accounts;
        }

        public async Task<IReadOnlyList<Account>> Handle(GetMyAccountsQuery request, CancellationToken cancellationToken)
        {
            return await _accounts.GetAllAsync(cancellationToken);
        }
    }
}
