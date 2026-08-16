using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Transfers;
using Wallet.Application.Transfers.ScheduleTransfer;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduledTransfersController : ControllerBase
    {
        private readonly ISender _sender;

        public ScheduledTransfersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<ActionResult<ScheduleTransferResponse>> Schedule(
            ScheduledTransferRequest request,
            [FromHeader(Name ="Idempotency-Key")] string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var scheduledTransferId = await _sender.Send(new ScheduleTransferCommand(
                request.SourceAccountId,
                request.DestinationAccountId,
                request.Amount,
                request.Currency,
                request.ScheduledFor,
                idempotencyKey
            ), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new ScheduleTransferResponse(scheduledTransferId));
        }
    }
}
