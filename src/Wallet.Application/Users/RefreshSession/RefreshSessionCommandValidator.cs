using FluentValidation;

namespace Wallet.Application.Users.RefreshSession
{
    public class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
    {
        public RefreshSessionCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .Matches("^wrt_[A-Za-z0-9_-]{43}$");
        }
    }
}
