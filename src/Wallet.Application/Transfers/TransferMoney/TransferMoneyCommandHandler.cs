using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand>
    {
        private readonly IUserRepository _users;
        private readonly IAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TransferService _transferService;
        private readonly ICurrentUser _currentUser;

        public TransferMoneyCommandHandler(IAccountRepository accounts, IUnitOfWork unitOfWork, TransferService transferService, IUserRepository users, ICurrentUser currentUser)
        {
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _transferService = transferService;
            _users = users;
            _currentUser = currentUser;
        }

        private async Task<Account> ResolveAccountAsync(Guid ownerId, string currency, CancellationToken cancellationToken)
        {
            var existing = await _accounts.GetByOwnerAndCurrencyAsync(ownerId, currency, cancellationToken);
            if (existing is not null)
                return existing;

            var account = new Account(Guid.NewGuid(), ownerId, currency);
            await _accounts.AddAsync(account, cancellationToken);
            return account;
        }
public async Task Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
        {
            if(!await _users.ExistsAsync(request.RecipientUserId, cancellationToken))
            {
                throw new InvalidTransferException("Recipient not found.");
            }
            if (request.RecipientUserId == _currentUser.UserId)
            {
                throw new InvalidTransferException("Cannot transfer to yourself.");
            }

            var currency = Money.NormalizeCurrency(request.Currency);
            var source = await ResolveAccountAsync(_currentUser.UserId, currency, cancellationToken);
            var destination = await ResolveAccountAsync(request.RecipientUserId, currency, cancellationToken);

            _transferService.Transfer(source, destination, new Money(request.Amount, currency));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }


    }
}