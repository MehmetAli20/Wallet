using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Transfers.TransferMoney;

public record TransferMoneyCommand(Guid SourceId, Guid DestinationId, decimal Amount, string Currency, string IdempotencyKey) : IRequest, IIdempotentRequest;