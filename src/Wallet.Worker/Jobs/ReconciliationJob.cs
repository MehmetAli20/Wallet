using Hangfire;
using MediatR;
using Wallet.Application.Reconciliation;

namespace Wallet.Worker.Jobs
{
    public class ReconciliationJob
    {
        private readonly ISender _sender;
        private readonly ILogger<ReconciliationJob> _logger;

        public ReconciliationJob(ISender sender, ILogger<ReconciliationJob> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var report = await _sender.Send(new GetReconciliationReportQuery(), cancellationToken);
            
            if (report.IsBalanced)
            {
                _logger.LogInformation("Reconciliation OK - {CurrencyCount} currencies checked, all balanced.", report.Balances.Count);
                return;
            }

            foreach (var balance in report.Balances.Where(b=>!b.IsBalanced))
            {
                _logger.LogCritical("RECONCILIATION FAILURE for {Currency}: Credits:{Credits}, Debits:{Debits}, discrepancy:{Discrepancy}", balance.Currency, balance.TotalCredits, balance.TotalDebits, balance.Discrepancy);
            }

            throw new InvalidOperationException("Reconciliation failed. See logs for details.");
        }
    }
}