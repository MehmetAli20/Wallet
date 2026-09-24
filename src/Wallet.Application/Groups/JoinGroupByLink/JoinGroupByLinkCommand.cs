using MediatR;
using Wallet.Domain.Groups;

namespace Wallet.Application.Groups.JoinGroupByLink
{
    public record JoinGroupByLinkCommand(string Token) : IRequest<Group>
    {
        public sealed override string ToString() =>
            $"{nameof(JoinGroupByLinkCommand)} {{ Token = *** }}";
    }
}
