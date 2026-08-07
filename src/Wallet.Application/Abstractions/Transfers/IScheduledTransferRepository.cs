using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Abstractions.Transfers
{
    public interface IScheduledTransferRepository
    {
        Task AddAsync(ScheduledTransfer scheduledTransfer, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ScheduledTransfer>> GetDueAsync(
            DateTimeOffset asOf, CancellationToken cancellationToken = default);
    }
}