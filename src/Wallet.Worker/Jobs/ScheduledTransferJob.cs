using MediatR;
using Wallet.Application.Transfers.ProcessDueScheduledTransfer;
using Wallet.Domain.Transfers;

namespace Wallet.Worker.Jobs
{
    public class ScheduledTransferJob
    {
        private readonly ISender _sender;
        private readonly ILogger<ScheduledTransferJob> _logger;

        public ScheduledTransferJob(ISender sender, ILogger<ScheduledTransferJob> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var processsedCount = await _sender.Send(new
                ProcessDueScheduledTransfersCommand(), cancellationToken);

            _logger.LogInformation("Processed {Count} due scheduled transfer(s).", processsedCount);
        }
    }
}