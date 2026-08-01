using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Accounts;

namespace Wallet.Application.Accounts.CreateAccount;
public record CreateAccountCommand(decimal Amount, string Currency) : IRequest<Account>;