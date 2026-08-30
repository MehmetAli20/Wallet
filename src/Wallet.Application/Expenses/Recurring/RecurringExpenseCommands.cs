using FluentValidation;
using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.Recurring
{
    public record CreateRecurringExpenseCommand(
        Guid GroupId,
        Guid PayerId,
        decimal Amount,
        string Description,
        RecurrenceInterval Interval,
        DateTimeOffset FirstOccurrence,
        IReadOnlyList<Guid> Participants,
        IReadOnlyDictionary<Guid, decimal> FixedShares,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;

    public record CancelRecurringExpenseCommand(Guid RecurringExpenseId) : IRequest;

    public record GetMyRecurringExpensesQuery : IRequest<IReadOnlyList<RecurringExpense>>;

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
            RuleFor(x => x.Participants).NotEmpty();
            RuleFor(x => x.IdempotencyKey).NotEmpty();
        }
    }

    public class CancelRecurringExpenseCommandValidator : AbstractValidator<CancelRecurringExpenseCommand>
    {
        public CancelRecurringExpenseCommandValidator()
        {
            RuleFor(x => x.RecurringExpenseId).NotEmpty();
        }
    }

    public class CreateRecurringExpenseCommandHandler
        : IRequestHandler<CreateRecurringExpenseCommand, Guid>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public CreateRecurringExpenseCommandHandler(
            IRecurringExpenseRepository recurring,
            IGroupRepository groups,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _recurring = recurring;
            _groups = groups;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateRecurringExpenseCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var recurring = RecurringExpense.Create(
                Guid.NewGuid(),
                group,
                request.PayerId,
                _currentUser.UserId,
                request.Amount,
                request.Description,
                request.Interval,
                request.FirstOccurrence,
                request.Participants,
                request.FixedShares);

            await _recurring.AddAsync(recurring, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return recurring.Id;
        }
    }

    public class CancelRecurringExpenseCommandHandler : IRequestHandler<CancelRecurringExpenseCommand>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly IUnitOfWork _unitOfWork;

        public CancelRecurringExpenseCommandHandler(
            IRecurringExpenseRepository recurring, IUnitOfWork unitOfWork)
        {
            _recurring = recurring;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            CancelRecurringExpenseCommand request, CancellationToken cancellationToken)
        {
            var recurring = await _recurring.GetByIdAsync(request.RecurringExpenseId, cancellationToken)
                ?? throw new ExpenseNotFoundException(request.RecurringExpenseId);

            recurring.Cancel();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public class GetMyRecurringExpensesQueryHandler
        : IRequestHandler<GetMyRecurringExpensesQuery, IReadOnlyList<RecurringExpense>>
    {
        private readonly IRecurringExpenseRepository _recurring;

        public GetMyRecurringExpensesQueryHandler(IRecurringExpenseRepository recurring)
        {
            _recurring = recurring;
        }

        public async Task<IReadOnlyList<RecurringExpense>> Handle(
            GetMyRecurringExpensesQuery request, CancellationToken cancellationToken) =>
            await _recurring.GetMineAsync(cancellationToken);
    }
}
