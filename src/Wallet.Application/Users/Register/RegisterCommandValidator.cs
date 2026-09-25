using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;
using Wallet.Domain.Users;

namespace Wallet.Application.Users.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50)
                .Matches("^[a-zA-Z0-9._-]+$")
                .WithMessage("Username can only contain letters, digits, dot, underscore and hyphen");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(254);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(72);

            RuleFor(x => x.DisplayName)
                .Must(name => User.TryNormalizeDisplayName(name, out _))
                .WithMessage($"Display name must be 1-{User.DisplayNameMaxLength} characters and cannot contain control or invisible characters.");
        }
    }
}