using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;
using Wallet.Domain.Reconciliation;

namespace Wallet.Application.Reconciliation
{
    public class GetReconciliationReportQueryHandler : IRequestHandler<GetReconciliationReportQuery, LedgerReconciliationReport>
    {
        private readonly IReconciliationRepository _reconciliation;
        
        public GetReconciliationReportQueryHandler(IReconciliationRepository reconciliation)
        {
            _reconciliation = reconciliation;
        }

        public async Task<LedgerReconciliationReport> Handle(GetReconciliationReportQuery request, CancellationToken cancellationToken)
        {
            var balances = await _reconciliation.GetCurrencyBalancesAsync(cancellationToken);
            var ledgerReport = LedgerReconciliationReport.From(balances);
            return ledgerReport;
        }
    }
}