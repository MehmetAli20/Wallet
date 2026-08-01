using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand>
    {
        private readonly IAccountRepository _account;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TransferService _transferService;

        public TransferMoneyCommandHandler(IAccountRepository account, IUnitOfWork unitOfWork, TransferService transferService)
        {
            _account = account;
            _unitOfWork = unitOfWork;
            _transferService = transferService;
        }

        public async Task Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
        {
            var sourceAccount = await _account.GetByIdAsync(request.SourceId, cancellationToken)
                ?? throw new AccountNotFoundException(request.SourceId);
            var destinationAccount = await _account.GetByIdAsync(request.DestinationId, cancellationToken)
                ?? throw new AccountNotFoundException(request.DestinationId);
            var amount = request.Amount;
            var currency = request.Currency;

            _transferService.Transfer(sourceAccount, destinationAccount, new Money(amount, currency));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}