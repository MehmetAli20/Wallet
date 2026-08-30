using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Activity.Responses;
using Wallet.Application.Activity.GetGroupActivity;
using Wallet.Application.Activity.GetUnreadActivity;
using Wallet.Application.Activity.MarkActivitySeen;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/groups")]
    [ApiController]
    public class GroupActivityController : ControllerBase
    {
        private readonly ISender _sender;

        public GroupActivityController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{groupId:guid}/activity")]
        public async Task<ActionResult<IReadOnlyList<ActivityEntryResponse>>> GetActivity(
            Guid groupId,
            [FromQuery] long? after,
            [FromQuery] int limit,
            CancellationToken cancellationToken)
        {
            var entries = await _sender.Send(
                new GetGroupActivityQuery(groupId, after, limit <= 0 ? 50 : Math.Min(limit, 200)),
                cancellationToken);

            return Ok(entries.Select(entry => entry.ToResponse()).ToList());
        }

        [HttpPost("{groupId:guid}/activity/seen")]
        public async Task<IActionResult> MarkActivitySeen(
            Guid groupId,
            MarkActivitySeenRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MarkActivitySeenCommand(groupId, request.Sequence), cancellationToken);

            return NoContent();
        }

        [HttpGet("activity/unread")]
        public async Task<ActionResult<IReadOnlyList<GroupUnreadCountResponse>>> GetUnread(
            CancellationToken cancellationToken)
        {
            var counts = await _sender.Send(new GetUnreadActivityQuery(), cancellationToken);

            return Ok(counts.Select(count => count.ToResponse()).ToList());
        }
    }
}
