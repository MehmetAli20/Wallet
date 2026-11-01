using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(254);

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MinimumLength(8)
                .Must(p => Encoding.UTF8.GetByteCount(p) <= IPasswordHasher.MaxPasswordBytes)
                .WithMessage($"Password must be at most {IPasswordHasher.MaxPasswordBytes} bytes in UTF-8.");

            RuleFor(x => x.DisplayName)
                .Must(name => User.TryNormalizeDisplayName(name, out _))
                .WithMessage($"Display name must be 1-{User.DisplayNameMaxLength} characters and cannot contain control or invisible characters.");
        }
    }
}