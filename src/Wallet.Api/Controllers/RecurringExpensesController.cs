using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Api.Contracts.Expenses.Responses;
using Wallet.Application.Expenses.Recurring;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecurringExpensesController : ControllerBase
    {
        private readonly ISender _sender;

        public RecurringExpensesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            CreateRecurringExpenseRequest request,
            CancellationToken cancellationToken)
        {
            var participants = request.Participants.Select(p => p.UserId).ToList();

            var fixedShares = request.Participants
                .Where(p => p.Share.HasValue)
                .ToDictionary(p => p.UserId, p => p.Share!.Value);

            var id = await _sender.Send(new CreateRecurringExpenseCommand(
                request.GroupId,
                request.PayerId,
                request.Amount,
                request.Description,
                request.Interval,
                request.FirstOccurrence,
                participants,
                fixedShares), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, id);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RecurringExpenseResponse>>> GetMine(
            CancellationToken cancellationToken)
        {
            var recurring = await _sender.Send(new GetMyRecurringExpensesQuery(), cancellationToken);

            return Ok(recurring.Select(r => r.ToResponse()).ToList());
        }

        [HttpDelete("{recurringExpenseId:guid}")]
        public async Task<IActionResult> Cancel(
            Guid recurringExpenseId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new CancelRecurringExpenseCommand(recurringExpenseId), cancellationToken);

            return NoContent();
        }
    }
}
