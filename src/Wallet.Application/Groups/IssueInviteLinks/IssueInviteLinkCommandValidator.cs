using FluentValidation;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.IssueInviteLinks
{
    public class IssueInviteLinkCommandValidator : AbstractValidator<IssueInviteLinkCommand>
    {
        public IssueInviteLinkCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();

            RuleFor(x => x.MaxUses)
                .InclusiveBetween(1, GroupInviteLink.MaxAllowedUses)
                .When(x => x.MaxUses.HasValue);

            RuleFor(x => x.MaxUses)
                .Null()
                .When(x => x.PlaceholderUserId.HasValue)
                .WithMessage("A placeholder claim token is always single use.");
        }
    }
}
