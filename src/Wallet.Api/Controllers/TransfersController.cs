using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Transfers;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Transfers;
using Wallet.Application.Transfers.TransferMoney;
using Wallet.Domain.Common;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransfersController : ControllerBase
    {
        private readonly ISender _sender;

        public TransfersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferRequest request, [FromHeader(Name ="Idempotency-Key")] string idempotencyKey, CancellationToken cancellationToken)
        {
            await _sender.Send(new TransferMoneyCommand(
                request.GroupId, request.RecipientUserId, request.Amount,
                idempotencyKey, request.OnBehalfOfUserId), cancellationToken);
            return NoContent();
        }
    }
}