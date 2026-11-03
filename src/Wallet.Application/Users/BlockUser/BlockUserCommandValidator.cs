using FluentValidation;

namespace Wallet.Application.Users.BlockUser
{
    public class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
    {
        public BlockUserCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
