using FluentValidation;

namespace Wallet.Application.Users.UnblockUser
{
    public class UnblockUserCommandValidator : AbstractValidator<UnblockUserCommand>
    {
        public UnblockUserCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
