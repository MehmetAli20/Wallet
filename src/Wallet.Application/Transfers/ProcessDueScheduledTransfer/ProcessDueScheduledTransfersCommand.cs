using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Application.Transfers.ProcessDueScheduledTransfer
{
    public record ProcessDueScheduledTransfersCommand : IRequest<int>;
}
