using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Invitations;
using Wallet.Api.Contracts.Invitations.Responses;
using Wallet.Application.Invitations.AcceptInvitation;
using Wallet.Application.Invitations.DeclineInvitation;
using Wallet.Application.Invitations.GetMyInvitations;

namespace Wallet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationsController : ControllerBase
    {
        private readonly ISender _sender;

        public InvitationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<InvitationResponse>>> GetMine(CancellationToken cancellationToken)
        {
            var invitations = await _sender.Send(new GetMyInvitationsQuery(), cancellationToken);
            return Ok(invitations.Select(i => i.ToResponse()).ToList());
        }

        [HttpPost("{invitationId:guid}/accept")]
        public async Task<IActionResult> Accept(Guid invitationId, CancellationToken cancellationToken)
        {
            await _sender.Send(new AcceptInvitationCommand(invitationId), cancellationToken);
            return NoContent();
        }

        [HttpDelete("{invitationId:guid}")]
        public async Task<IActionResult> Decline(Guid invitationId, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeclineInvitationCommand(invitationId), cancellationToken);
            return NoContent();
        }
    }
}
