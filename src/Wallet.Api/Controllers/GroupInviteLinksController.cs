using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Groups;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Application.Groups.GetGroupPlaceholders;
using Wallet.Application.Groups.IssueInviteLinks;
using Wallet.Application.Groups.JoinGroupByLink;
using Wallet.Application.Groups.RevokeInviteLink;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/groups")]
    [ApiController]
    public class GroupInviteLinksController : ControllerBase
    {
        private readonly ISender _sender;

        public GroupInviteLinksController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{groupId:guid}/placeholders")]
        public async Task<ActionResult<IReadOnlyList<GroupPlaceholderResponse>>> GetPlaceholders(
            Guid groupId, CancellationToken cancellationToken)
        {
            var placeholders = await _sender.Send(
                new GetGroupPlaceholdersQuery(groupId), cancellationToken);

            return Ok(placeholders
                .Select(p => new GroupPlaceholderResponse(p.Id, p.DisplayName))
                .ToList());
        }

        [HttpPost("{groupId:guid}/invite-links")]
        public async Task<ActionResult<IssuedTokenResponse>> Issue(
            Guid groupId, IssueInviteLinkRequest request, CancellationToken cancellationToken)
        {
            var issued = await _sender.Send(
                new IssueInviteLinkCommand(groupId, request.PlaceholderUserId, request.MaxUses),
                cancellationToken);

            return Ok(issued.ToResponse());
        }

        [HttpDelete("{groupId:guid}/invite-links/{linkId:guid}")]
        public async Task<IActionResult> Revoke(
            Guid groupId, Guid linkId, CancellationToken cancellationToken)
        {
            await _sender.Send(new RevokeInviteLinkCommand(groupId, linkId), cancellationToken);

            return NoContent();
        }

        [HttpPost("invite-links/join")]
        public async Task<ActionResult<GroupResponse>> Join(
            JoinGroupRequest request, CancellationToken cancellationToken)
        {
            var group = await _sender.Send(new JoinGroupByLinkCommand(request.Token), cancellationToken);

            return Ok(group.ToResponse());
        }
    }
}
