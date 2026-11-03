using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Users;
using Wallet.Application.Users.GetOpenReports;
using Wallet.Application.Users.ResolveReport;
using Wallet.Domain.Users;

namespace Wallet.Api.Controllers
{
    [Route("api/v1/admin/reports")]
    [ApiController]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class AdminReportsController : ControllerBase
    {
        private readonly ISender _sender;

        public AdminReportsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<List<OpenReportResponse>>> GetOpen(CancellationToken cancellationToken)
        {
            var reports = await _sender.Send(new GetOpenReportsQuery(), cancellationToken);

            return reports.Select(r => r.ToOpenReportResponse()).ToList();
        }

        [HttpPost("{reportId:guid}/resolution")]
        public async Task<IActionResult> Resolve(Guid reportId, CancellationToken cancellationToken)
        {
            await _sender.Send(new ResolveReportCommand(reportId), cancellationToken);

            return NoContent();
        }
    }
}
