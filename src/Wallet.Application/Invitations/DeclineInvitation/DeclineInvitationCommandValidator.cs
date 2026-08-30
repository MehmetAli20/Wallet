using FluentValidation;

namespace Wallet.Application.Invitations.DeclineInvitation;

public class DeclineInvitationCommandValidator : AbstractValidator<DeclineInvitationCommand>
{
    public DeclineInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId).NotEmpty();
    }
}
