using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Accounts;
using Wallet.Api.Contracts.Accounts.Responses;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Accounts.GetAccountById;
using Wallet.Application.Accounts.GetAccountEntries;
using Wallet.Application.Accounts.GetMyAccounts;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

namespace Wallet.Api.Controllers
{
    
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ISender _sender;

        public AccountsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var account = await _sender.Send(new GetAccountByIdQuery(id), cancellationToken);
            if(account is null)
            {
                return NotFound();
            }
            return account.ToResponse();
        }

        [HttpGet("{accountId:guid}/entries")]
        public async Task<ActionResult<IReadOnlyList<LedgerEntryResponse>>> GetEntries(
            Guid accountId,
            [FromQuery] int skip,
            [FromQuery] int take,
            CancellationToken cancellationToken)
        {
            var entries = await _sender.Send(new GetAccountEntriesQuery(
                accountId,
                skip < 0 ? 0 : skip,
                take <= 0 ? 50 : Math.Min(take, 200)), cancellationToken);

            return Ok(entries.Select(entry => entry.ToResponse()).ToList());
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AccountListResponse>>> GetAllMyAccounts(CancellationToken cancellationToken)
        {
            var accounts = await _sender.Send(new GetMyAccountsQuery(), cancellationToken);

            return Ok(accounts.Select(account => account.ToListResponse()).ToList());
        }
    }
}