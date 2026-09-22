using MediatR;

namespace Wallet.Application.Groups.RemoveGroupMember
{
    public record RemoveGroupMemberCommand(Guid GroupId, Guid UserId) : IRequest;
}
