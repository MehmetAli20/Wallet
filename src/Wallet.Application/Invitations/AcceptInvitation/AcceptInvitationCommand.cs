using MediatR;

namespace Wallet.Application.Invitations.AcceptInvitation;

public record AcceptInvitationCommand(Guid InvitationId) : IRequest;
