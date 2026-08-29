using MediatR;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Expenses;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Accounts;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Expenses;

namespace Wallet.Application.Expenses.Recurring
{
    public record ProcessDueRecurringExpensesCommand(DateTimeOffset AsOf) : IRequest<int>;

    public record PostRecurringOccurrenceCommand(
        Guid RecurringExpenseId,
        DateTimeOffset Occurrence,
        string IdempotencyKey) : IRequest<Guid>, IIdempotentRequest;

    public class ProcessDueRecurringExpensesCommandHandler
        : IRequestHandler<ProcessDueRecurringExpensesCommand, int>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly ISender _sender;
        private readonly ILogger<ProcessDueRecurringExpensesCommandHandler> _logger;

        public ProcessDueRecurringExpensesCommandHandler(
            IRecurringExpenseRepository recurring,
            ISender sender,
            ILogger<ProcessDueRecurringExpensesCommandHandler> logger)
        {
            _recurring = recurring;
            _sender = sender;
            _logger = logger;
        }

        public async Task<int> Handle(
            ProcessDueRecurringExpensesCommand request, CancellationToken cancellationToken)
        {
            var due = await _recurring.GetDueAsync(request.AsOf, cancellationToken);
            var posted = 0;

            foreach (var recurring in due)
            {
                var occurrence = recurring.NextOccurrence;
                var key = $"recurring:{recurring.Id:N}:{occurrence:yyyyMMdd}";

                try
                {
                    await _sender.Send(
                        new PostRecurringOccurrenceCommand(recurring.Id, occurrence, key),
                        cancellationToken);

                    posted++;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Recurring expense {RecurringExpenseId} due {Occurrence} could not be posted.",
                        recurring.Id,
                        occurrence);
                }
            }

            return posted;
        }
    }

    public class PostRecurringOccurrenceCommandHandler
        : IRequestHandler<PostRecurringOccurrenceCommand, Guid>
    {
        private readonly IRecurringExpenseRepository _recurring;
        private readonly IExpenseRepository _expenses;
        private readonly IGroupRepository _groups;
        private readonly IAccountRepository _accounts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ExpensePostingService _postingService;

        public PostRecurringOccurrenceCommandHandler(
            IRecurringExpenseRepository recurring,
            IExpenseRepository expenses,
            IGroupRepository groups,
            IAccountRepository accounts,
            IUnitOfWork unitOfWork,
            ExpensePostingService postingService)
        {
            _recurring = recurring;
            _expenses = expenses;
            _groups = groups;
            _accounts = accounts;
            _unitOfWork = unitOfWork;
            _postingService = postingService;
        }

        public async Task<Guid> Handle(
            PostRecurringOccurrenceCommand request, CancellationToken cancellationToken)
        {
            var recurring = await _recurring.GetByIdAsync(request.RecurringExpenseId, cancellationToken)
                ?? throw new ExpenseNotFoundException(request.RecurringExpenseId);

            var group = await _groups.GetByIdAsync(recurring.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(recurring.GroupId);

            var participants = recurring.ParticipantIds();

            var expense = Expense.Create(
                Guid.NewGuid(),
                group,
                recurring.PayerId,
                recurring.CreatedBy,
                recurring.Amount.Amount,
                recurring.Description,
                request.Occurrence,
                participants,
                recurring.FixedShares());

            var accounts = new Dictionary<Guid, Account>();

            foreach (var participantId in participants.Distinct())
            {
                accounts[participantId] = await ResolveAccountAsync(
                    participantId, group.Currency, cancellationToken);
            }

            _postingService.Post(expense, accounts);
            await _expenses.AddAsync(expense, cancellationToken);

            recurring.Advance();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return expense.Id;
        }

        private async Task<Account> ResolveAccountAsync(
            Guid ownerId, string currency, CancellationToken cancellationToken)
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
