using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Reconciliation;

namespace Wallet.Application.Reconciliation
{
    public record GetReconciliationReportQuery : IRequest<LedgerReconciliationReport>;
}
