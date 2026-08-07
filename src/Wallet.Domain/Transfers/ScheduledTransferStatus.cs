using System;
using System.Collections.Generic;
using System.Text;

namespace Wallet.Domain.Transfers
{
    public enum ScheduledTransferStatus
    {
        Pending,
        Executed,
        Failed
    }
}