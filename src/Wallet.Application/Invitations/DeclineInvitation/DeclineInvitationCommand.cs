using MediatR;

namespace Wallet.Application.Invitations.DeclineInvitation;

public record DeclineInvitationCommand(Guid InvitationId) : IRequest;
