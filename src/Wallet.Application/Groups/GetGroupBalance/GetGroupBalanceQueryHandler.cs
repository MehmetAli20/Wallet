using MediatR;
using Wallet.Application.Abstractions.Groups;
using Wallet.Domain.Exceptions;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.GetGroupBalance
{
    public class GetGroupBalanceQueryHandler : IRequestHandler<GetGroupBalanceQuery, GroupBalanceReport>
    {
        private readonly IGroupRepository _groups;
        private readonly IGroupBalanceRepository _balances;

        public GetGroupBalanceQueryHandler(IGroupRepository groups, IGroupBalanceRepository balances)
        {
            _groups = groups;
            _balances = balances;
        }

        public async Task<GroupBalanceReport> Handle(GetGroupBalanceQuery request, CancellationToken cancellationToken)
        {
            var group = await _groups.GetByIdAsync(request.GroupId, cancellationToken)
                ?? throw new GroupNotFoundException(request.GroupId);

            var measured = await _balances.GetPositionsAsync(request.GroupId, cancellationToken);
            var byUser = measured.ToDictionary(p => p.UserId, p => p.Net);

            var positions = group.Members
                .Where(m => m.Status == GroupMemberStatus.Active)
                .Select(m => new MemberPosition(m.UserId, byUser.TryGetValue(m.UserId, out var net) ? net : 0m))
                .OrderBy(p => p.UserId)
                .ToList();

            var debts = request.Simplify
                ? DebtSimplifier.Simplify(positions)
                : await _balances.GetDebtsAsync(request.GroupId, cancellationToken);

            return new GroupBalanceReport(group.Id, group.Currency, positions, debts);
        }
    }
}
