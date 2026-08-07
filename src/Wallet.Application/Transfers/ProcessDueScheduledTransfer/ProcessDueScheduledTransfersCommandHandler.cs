using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Transfers;
using Wallet.Application.Transfers.TransferMoney;

namespace Wallet.Application.Transfers.ProcessDueScheduledTransfer
{
    public class ProcessDueScheduledTransfersCommandHandler
        : IRequestHandler<ProcessDueScheduledTransfersCommand, int>
    {
        private readonly IScheduledTransferRepository _scheduledTransfer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISender _sender;

        public ProcessDueScheduledTransfersCommandHandler(
            IScheduledTransferRepository scheduledTransfer,
            IUnitOfWork unitOfWork,
            ISender sender)
        {
            _scheduledTransfer = scheduledTransfer;
            _unitOfWork = unitOfWork;
            _sender = sender;
        }

        public async Task<int> Handle(ProcessDueScheduledTransfersCommand request, CancellationToken cancellationToken)
        {
            var due = await _scheduledTransfer.GetDueAsync(DateTimeOffset.UtcNow , cancellationToken);
            foreach(var scheduled in due)
            {
                try
                {
                    await _sender.Send(
                        new TransferMoneyCommand(
                            scheduled.SourceAccountId,
                            scheduled.DestinationAccountId,
                            scheduled.Amount.Amount,
                            scheduled.Amount.Currency,
                            $"scheduled-{scheduled.Id}"),
                        cancellationToken);
                    scheduled.MarkExecuted();
                }
                catch (Exception ex)
                {
                    scheduled.MarkFailed(ex.Message);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return due.Count;
        }
    }
}
