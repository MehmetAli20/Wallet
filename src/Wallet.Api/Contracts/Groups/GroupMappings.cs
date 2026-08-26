using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Domain.Groups;

namespace Wallet.Api.Contracts.Groups
{
    public static class GroupMappings
    {
        public static GroupResponse ToResponse(this Group group) =>
            new(group.Id,
                group.Kind.ToString(),
                group.Name,
                group.Currency,
                group.Members
                    .Select(m => new GroupMemberResponse(m.UserId, m.Role.ToString(), m.Status.ToString()))
                    .ToList());

        public static GroupBalanceResponse ToResponse(this GroupBalanceReport report) =>
            new(report.GroupId,
                report.Currency,
                report.Positions.Select(p => new MemberPositionResponse(p.UserId, p.Net)).ToList(),
                report.Debts.Select(d => new PairwiseDebtResponse(d.DebtorId, d.CreditorId, d.Amount)).ToList());
    }
}