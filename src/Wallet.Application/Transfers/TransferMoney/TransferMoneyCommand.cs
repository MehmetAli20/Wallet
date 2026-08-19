using MediatR;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Transfers.TransferMoney;

public record TransferMoneyCommand(Guid RecipientUserId, decimal Amount, string Currency, string IdempotencyKey) : IRequest, IIdempotentRequest;