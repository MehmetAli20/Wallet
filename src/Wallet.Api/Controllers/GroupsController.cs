using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Activity.Responses;
using Wallet.Api.Contracts.Common;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Api.Contracts.Groups;
using Wallet.Api.Contracts.Groups.Requests;
using Wallet.Api.Contracts.Groups.Responses;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Activity.GetGroupActivity;
using Wallet.Application.Expenses.GetGroupExpenses;
using Wallet.Application.Activity.GetUnreadActivity;
using Wallet.Application.Activity.MarkActivitySeen;
using Wallet.Application.Groups.CreateGroup;
using Wallet.Api.Contracts.Placeholders;
using Wallet.Application.Groups.EnsurePair;
using Wallet.Application.Groups.Placeholders;
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
        private readonly ICurrentUser _currentUser;

        public GroupsController(ISender sender, ICurrentUser currentUser)
        {
            _sender = sender;
            _currentUser = currentUser;
        }

        [HttpPost]
        public async Task<ActionResult<CreatedResponse>> Create(CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(new CreateGroupCommand(request.Name, request.Currency), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new CreatedResponse(id));
        }

        [HttpPost("pairs")]
        public async Task<ActionResult<CreatedResponse>> EnsurePair(
            EnsurePairRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new EnsurePairCommand(request.UserId, request.Currency), cancellationToken);

            var body = new CreatedResponse(result.GroupId);

            return result.Created
                ? StatusCode(StatusCodes.Status201Created, body)
                : Ok(body);
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

        [HttpPost("{groupId:guid}/placeholders")]
        public async Task<ActionResult<CreatedResponse>> AddPlaceholder(
            Guid groupId,
            AddPlaceholderRequest request,
            CancellationToken cancellationToken)
        {
            var id = await _sender.Send(
                new AddPlaceholderCommand(groupId, request.DisplayName), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new CreatedResponse(id));
        }

        [HttpGet("{groupId:guid}/expenses")]
        public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> GetExpenses(
            Guid groupId,
            [FromQuery] bool includeReversed,
            [FromQuery] int skip,
            [FromQuery] int take,
            CancellationToken cancellationToken)
        {
            var expenses = await _sender.Send(new GetGroupExpensesQuery(
                groupId,
                includeReversed,
                skip < 0 ? 0 : skip,
                take <= 0 ? 50 : Math.Min(take, 200)), cancellationToken);

            return Ok(expenses.Select(e => e.ToResponse(_currentUser.UserId)).ToList());
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
