using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Settlements;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Settlements;
using Wallet.Domain.Transfers;

namespace Wallet.Application.Transfers.TransferMoney
{
    public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand>
    {
        private readonly IAccountRepository _accounts;
        private readonly IGroupRepository _groups;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TransferService _transferService;
        private readonly ISettlementRepository _settlements;
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _users;

        public TransferMoneyCommandHandler(
            IAccountRepository accounts,
            IGroupRepository groups,
            IUnitOfWork unitOfWork,
            TransferService transferService,
            ISettlementRepository settlements,
            ICurrentUser currentUser,
            IUserRepository users)
        {
            _accounts = accounts;
            _groups = groups;
            _unitOfWork = unitOfWork;
            _transferService = transferService;
            _settlements = settlements;
            _currentUser = currentUser;
            _users = users;
        }

        public async Task Handle(TransferMoneyCommand request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var payerId = _currentUser.UserId;

            if (request.OnBehalfOfUserId is { } onBehalfOf)
            {
                var party = await _users.GetByIdAsync(onBehalfOf, cancellationToken)
                    ?? throw new InvalidTransferException("Payer not found.");

                if (!party.IsPlaceholder)
                    throw new InvalidTransferException(
                        "A settlement can only be recorded on behalf of a placeholder.");

                if (!group.IsActiveMember(onBehalfOf))
                    throw new InvalidTransferException(
                        "The payer is not an active member of this group.");

                payerId = onBehalfOf;
            }

            if (request.RecipientUserId == payerId)
                throw new InvalidTransferException("Cannot transfer to yourself.");

            if (!group.IsActiveMember(request.RecipientUserId))
                throw new InvalidTransferException("The recipient is not an active member of this group.");

            var payer = await ResolveAccountAsync(payerId, group.Currency, cancellationToken);
            var payee = await ResolveAccountAsync(request.RecipientUserId, group.Currency, cancellationToken);

            _transferService.Settle(payer, payee, new Money(request.Amount, group.Currency), group.Id);

            var settlement = Settlement.Record(
                Guid.NewGuid(), group, payerId, request.RecipientUserId,
                request.Amount, DateTimeOffset.UtcNow);

            await _settlements.AddAsync(settlement, cancellationToken);

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
