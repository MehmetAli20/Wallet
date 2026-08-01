using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

namespace Wallet.Application.Accounts.CreateAccount
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Account>
    {
        private readonly IAccountRepository _account;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAccountCommandHandler(IAccountRepository account, IUnitOfWork unitOfWork)
        {
            _account = account;
            _unitOfWork = unitOfWork;
        }

        public async Task<Account> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var account = new Account(Guid.NewGuid(), new Money(request.Amount, request.Currency));

            await _account.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return account;
        }
    }
}
