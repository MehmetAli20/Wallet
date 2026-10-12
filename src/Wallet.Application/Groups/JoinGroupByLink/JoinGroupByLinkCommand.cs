using MediatR;

namespace Wallet.Application.Groups.JoinGroupByLink
{
    public record JoinGroupByLinkCommand(string Token) : IRequest<GroupView>
    {
        public sealed override string ToString() =>
            $"{nameof(JoinGroupByLinkCommand)} {{ Token = *** }}";
    }
}
