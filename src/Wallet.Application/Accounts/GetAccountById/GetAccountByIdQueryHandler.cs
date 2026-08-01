using MediatR;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Accounts.GetAccountById
{
    public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, Account?>
    {
        private readonly IAccountRepository _account;

        public GetAccountByIdQueryHandler(IAccountRepository account)
        {
            _account = account;
        }

        public Task<Account?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            var account = _account.GetByIdAsync(request.AccountId, cancellationToken);
            return account;
        }
    }
}
