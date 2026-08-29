using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.ReverseExpense
{
    public class ReverseExpenseCommandHandler : IRequestHandler<ReverseExpenseCommand>
    {
        private readonly IExpenseRepository _expenses;
        private readonly IGroupRepository _groups;
        private readonly IAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ExpensePostingService _postingService;
        private readonly ICurrentUser _currentUser;

        public ReverseExpenseCommandHandler(
            IExpenseRepository expenses,
            IGroupRepository groups,
            IAccountRepository accounts,
            IUnitOfWork unitOfWork,
            ExpensePostingService postingService,
            ICurrentUser currentUser)
        {
            _expenses = expenses;
            _groups = groups;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _postingService = postingService;
            _currentUser = currentUser;
        }

        public async Task Handle(ReverseExpenseCommand request, CancellationToken cancellationToken)
        {
            var expense = await _expenses.GetByIdAsync(request.ExpenseId, cancellationToken)
                ?? throw new ExpenseNotFoundException(request.ExpenseId);

            var group = await _groups.GetByIdAsync(expense.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(expense.GroupId);

            var accounts = new Dictionary<Guid, Account>();

            foreach (var participantId in expense.Splits.Select(s => s.ParticipantId).Distinct())
            {
                accounts[participantId] = await ResolveAccountAsync(participantId, group.Currency, cancellationToken);
            }

            expense.Reverse(_currentUser.UserId, DateTimeOffset.UtcNow, request.Reason);
            _postingService.Reverse(expense, accounts);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<Account> ResolveAccountAsync(Guid ownerId, string currency, CancellationToken cancellationToken)
        {
            var existing = await _accounts.GetByOwnerAndCurrencyAsync(ownerId, currency, cancellationToken);
            if (existing is not null)
                return existing;

            var account = new Account(Guid.NewGuid(), ownerId, currency);
            await _accounts.AddAsync(account, cancellationToken);
            return account;
        }
    }
}
