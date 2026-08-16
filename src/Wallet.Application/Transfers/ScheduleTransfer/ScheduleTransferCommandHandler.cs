using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Transfers;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers.ScheduleTransfer
{
    public class ScheduleTransferCommandHandler : IRequestHandler<ScheduleTransferCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IScheduledTransferRepository _scheduledTransfers;
        private readonly IAccountRepository _accounts;
        public ScheduleTransferCommandHandler(
            IUnitOfWork unitOfWork,
            IAccountRepository accounts,
            IScheduledTransferRepository scheduledTransfers)
        {
            _unitOfWork = unitOfWork;
            _scheduledTransfers = scheduledTransfers;
            _accounts = accounts;
        }

        public async Task<Guid> Handle(ScheduleTransferCommand request, CancellationToken cancellationToken)
        {

            _ = await _accounts.GetByIdAsync(request.SourceAccountId, cancellationToken) ?? throw new AccountNotFoundException(request.SourceAccountId);
            _ = await _accounts.GetByIdAsync(request.DestinationAccountId, cancellationToken) ?? throw new AccountNotFoundException(request.DestinationAccountId);

            var scheduledTransfer = new ScheduledTransfer(
                Guid.NewGuid(),
                request.SourceAccountId,
                request.DestinationAccountId,
                new Money(request.Amount, request.Currency),
                request.ScheduledFor
                );

            await _scheduledTransfers.AddAsync(scheduledTransfer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return scheduledTransfer.Id;
        }
    }
}
