using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Accounts.GetAccountById;
public record GetAccountByIdQuery(Guid AccountId) : IRequest<Account?>;
