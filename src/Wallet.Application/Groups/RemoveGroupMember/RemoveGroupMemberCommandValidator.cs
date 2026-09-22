using FluentValidation;

namespace Wallet.Application.Groups.RemoveGroupMember
{
    public class RemoveGroupMemberCommandValidator : AbstractValidator<RemoveGroupMemberCommand>
    {
        public RemoveGroupMemberCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
