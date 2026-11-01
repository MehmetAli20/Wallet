using FluentValidation;
using Wallet.Application.Abstractions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.ReviseExpense
{
    public class ReviseExpenseCommandValidator : AbstractValidator<ReviseExpenseCommand>
    {
        public ReviseExpenseCommandValidator()
        {
            RuleFor(x => x.ExpenseId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OccurredAt).NotEqual(default(DateTimeOffset));
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
