using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Groups;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Users;
using Wallet.Domain.Common;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.EnsurePair
{
    public class EnsurePairCommandHandler : IRequestHandler<EnsurePairCommand, PairResult>
    {
        private readonly IGroupRepository _groups;
        private readonly ContactPolicy _contacts;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public EnsurePairCommandHandler(
            IGroupRepository groups,
            ContactPolicy contacts,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _groups = groups;
            _contacts = contacts;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<PairResult> Handle(EnsurePairCommand request, CancellationToken cancellationToken)
        {
            if (request.OtherUserId == _currentUser.UserId)
                throw new InvalidGroupOperationException("A pair needs two different people.");

            await _contacts.EnsureCanReachAsync(request.OtherUserId, cancellationToken);

            var currency = Money.NormalizeCurrency(request.Currency);

            var existing = await _groups.GetPairAsync(
                _currentUser.UserId, request.OtherUserId, currency, cancellationToken);

            if (existing is not null)
                return new PairResult(existing.Id, Created: false);

            var pair = Group.CreatePair(
                Guid.NewGuid(), currency, _currentUser.UserId, request.OtherUserId);

            await _groups.AddAsync(pair, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PairResult(pair.Id, Created: true);
        }
    }
}
