using MediatR;
using Wallet.Domain.Groups;

namespace Wallet.Application.Invitations.GetMyInvitations;

public record GetMyInvitationsQuery : IRequest<IReadOnlyList<PendingInvitation>>;
