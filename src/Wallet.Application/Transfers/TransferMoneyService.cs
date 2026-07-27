using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers
{
    public class TransferMoneyService
    {
        private readonly IAccountRepository _account;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TransferService _transferService;

        public TransferMoneyService(IAccountRepository account, IUnitOfWork unitOfWork, TransferService transferService)
        {
            _account = account;
            _unitOfWork = unitOfWork;
            _transferService = transferService;
        }

        public async Task TransferAsync(Guid sourceId, Guid destinationId, Money amount, CancellationToken cancellationToken = default)
        {
            var sourceAccount = await _account.GetByIdAsync(sourceId, cancellationToken) ?? throw new InvalidOperationException($"{sourceId} was not found.");
            var destinationAccount = await _account.GetByIdAsync(destinationId, cancellationToken) ?? throw new InvalidOperationException($"{destinationId} was not found.");

            _transferService.Transfer(sourceAccount, destinationAccount, amount);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}