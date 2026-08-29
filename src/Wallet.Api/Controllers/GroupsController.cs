using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Activity.Responses;
using Wallet.Api.Contracts.Groups;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Application.Activity.GetGroupActivity;
using Wallet.Application.Activity.GetUnreadActivity;
using Wallet.Application.Activity.MarkActivitySeen;
using Wallet.Application.Groups.CreateGroup;
using Wallet.Application.Groups.GetGroupBalance;
using Wallet.Application.Groups.GetMyGroups;
using Wallet.Application.Groups.InviteToGroup;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly ISender _sender;

        public GroupsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create(CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(new CreateGroupCommand(request.Name, request.Currency), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, id);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetMyGroups(CancellationToken cancellationToken)
        {
            var groups = await _sender.Send(new GetMyGroupsQuery(), cancellationToken);
            return Ok(groups.Select(g => g.ToResponse()).ToList());
        }

        [HttpPost("{groupId:guid}/members")]
        public async Task<IActionResult> Invite(Guid groupId, InviteToGroupRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new InviteToGroupCommand(groupId, request.UserId), cancellationToken);
            return NoContent();
        }

        [HttpGet("{groupId:guid}/balance")]
        public async Task<ActionResult<GroupBalanceResponse>> GetBalance(
            Guid groupId,
            [FromQuery] bool simplify,
            CancellationToken cancellationToken)
        {
            var report = await _sender.Send(new GetGroupBalanceQuery(groupId, simplify), cancellationToken);
            return Ok(report.ToResponse());
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
