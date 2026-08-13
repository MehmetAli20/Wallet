using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

namespace Wallet.Application.Accounts.CreateAccount
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Account>
    {
        private readonly IAccountRepository _account;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public CreateAccountCommandHandler(IAccountRepository account, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        {
            _account = account;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Account> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            var account = new Account(id: Guid.NewGuid(), ownerId: _currentUser.UserId, openingBalance: new Money(request.Amount, request.Currency));

            await _account.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return account;
        }
    }
}