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
                _logger.LogInformation("Reconciliation OK - {CurrencyCount} currencies, {AccountCount} accounts checked.",
                    report.LedgerTotals.Count, report.NetPositions.Count);
                return;
            }

            foreach (var t in report.LedgerTotals.Where(x => !x.IsBalanced))
                _logger.LogCritical("LEDGER UNBALANCED {Currency}: credits {Credits}, debits {Debits}, diff {Discrepancy}",
                    t.Currency, t.TotalCredits, t.TotalDebits, t.Discrepancy);

            foreach (var p in report.NetPositions.Where(x => !x.IsBalanced))
                _logger.LogCritical("POSITIONS DO NOT NET TO ZERO {Currency}: total {Total}",
                    p.Currency, p.TotalBalance);

            foreach (var d in report.AccountDiscrepancies)
                _logger.LogCritical("ACCOUNT BALANCE DRIFT {AccountId} {Currency}: balance {Balance}, ledger {LedgerNet}, diff {Difference}",
                    d.AccountId, d.Currency, d.Balance, d.LedgerNet, d.Difference);

            foreach (var c in report.ContextBalances.Where(x => !x.IsBalanced))
                _logger.LogCritical("CONTEXT DOES NOT NET TO ZERO {GroupId}: net {Net}",
                    c.GroupId, c.Net);

            throw new InvalidOperationException("Reconciliation failed. See logs for details.");
        }
    }
}