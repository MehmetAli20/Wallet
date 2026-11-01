using FluentValidation;
using Wallet.Application.Abstractions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.CreateRecurringExpense
{
    public class CreateRecurringExpenseCommandValidator : AbstractValidator<CreateRecurringExpenseCommand>
    {
        public CreateRecurringExpenseCommandValidator()
        {
            RuleFor(x => x.GroupId).NotEmpty();
            RuleFor(x => x.PayerId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Interval).IsInEnum();
            RuleFor(x => x.FirstOccurrence).NotEqual(default(DateTimeOffset));
            RuleFor(x => x.Participants)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(p => p.Count <= Expense.MaxParticipants)
                .WithMessage($"An expense can have at most {Expense.MaxParticipants} participants.")
                .Must(p => p.Distinct().Count() == p.Count)
                .WithMessage("A participant cannot appear twice.");
            RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(IIdempotentRequest.KeyMaxLength);
        }
    }
}
