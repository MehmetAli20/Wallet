using MediatR;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Transfers.TransferMoney;

public record TransferMoneyCommand(
    Guid GroupId,
    Guid RecipientUserId,
    decimal Amount,
    string IdempotencyKey,
    Guid? OnBehalfOfUserId = null) : IRequest, IIdempotentRequest;