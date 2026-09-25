using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.CreateRecurringExpense
{
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
}
