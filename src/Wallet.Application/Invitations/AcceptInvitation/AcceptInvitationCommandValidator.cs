using FluentValidation;

namespace Wallet.Application.Invitations.AcceptInvitation;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}
