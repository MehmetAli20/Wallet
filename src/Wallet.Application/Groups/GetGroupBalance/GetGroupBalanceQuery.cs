using MediatR;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.GetGroupBalance;

public record GetGroupBalanceQuery(Guid GroupId, bool Simplify) : IRequest<GroupBalanceReport>;
