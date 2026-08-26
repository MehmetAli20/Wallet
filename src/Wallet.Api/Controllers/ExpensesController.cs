using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Expenses.Requests;
using Wallet.Application.Expenses.CreateExpense;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly ISender _sender;

        public ExpensesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            CreateExpenseRequest request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var participants = request.Participants.Select(p => p.UserId).ToList();

            var fixedShares = request.Participants
                .Where(p => p.Share.HasValue)
                .ToDictionary(p => p.UserId, p => p.Share!.Value);

            var id = await _sender.Send(new CreateExpenseCommand(
                request.GroupId,
                request.PayerId,
                request.Amount,
                request.Description,
                request.OccurredAt,
                participants,
                fixedShares,
                idempotencyKey), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, id);
        }
    }
}