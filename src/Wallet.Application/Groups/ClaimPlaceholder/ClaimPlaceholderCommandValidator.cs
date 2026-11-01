using FluentValidation;
using System.Text;
using Wallet.Application.Abstractions.Users;

namespace Wallet.Application.Groups.ClaimPlaceholder
{
    public class ClaimPlaceholderCommandValidator : AbstractValidator<ClaimPlaceholderCommand>
    {
        public ClaimPlaceholderCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MinimumLength(8)
                .Must(p => Encoding.UTF8.GetByteCount(p) <= IPasswordHasher.MaxPasswordBytes)
                .WithMessage($"Password must be at most {IPasswordHasher.MaxPasswordBytes} bytes in UTF-8.");
        }
    }
}
