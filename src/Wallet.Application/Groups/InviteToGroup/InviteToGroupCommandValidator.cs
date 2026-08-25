using FluentValidation;

namespace Wallet.Application.Groups.InviteToGroup
{
    public class InviteToGroupCommandValidator : AbstractValidator<InviteToGroupCommand>
    {
        public InviteToGroupCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}