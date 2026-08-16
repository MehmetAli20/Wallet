using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Accounts.GetMyAccounts
{
    public record GetMyAccountsQuery : IRequest<IReadOnlyList<Account>>;
}
