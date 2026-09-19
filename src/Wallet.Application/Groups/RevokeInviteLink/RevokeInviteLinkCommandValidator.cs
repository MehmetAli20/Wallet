using FluentValidation;

namespace Wallet.Application.Groups.RevokeInviteLink
{
    public class RevokeInviteLinkCommandValidator : AbstractValidator<RevokeInviteLinkCommand>
    {
        public RevokeInviteLinkCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.LinkId).NotEmpty();
        }
    }
}
