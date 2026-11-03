using FluentValidation;

namespace Wallet.Application.Users.DeleteCurrentUser
{
    public class DeleteCurrentUserCommandValidator : AbstractValidator<DeleteCurrentUserCommand>
    {
        public DeleteCurrentUserCommandValidator()
        {
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}
