using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Application.Abstractions.Users;
using Wallet.Application.Expenses.GetGroupExpenses;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/groups")]
    [ApiController]
    public class GroupExpensesController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ICurrentUser _currentUser;

        public GroupExpensesController(ISender sender, ICurrentUser currentUser)
        {
            _sender = sender;
            _currentUser = currentUser;
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
    }
}
