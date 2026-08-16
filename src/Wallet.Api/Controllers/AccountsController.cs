using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts.Accounts;
using Wallet.Api.Contracts.Accounts.Requests;
using Wallet.Api.Contracts.Accounts.Responses;
using Wallet.Application.Abstractions;
using Wallet.Application.Abstractions.Accounts;
using Wallet.Application.Accounts.CreateAccount;
using Wallet.Application.Accounts.GetAccountById;
using Wallet.Application.Accounts.GetMyAccounts;
using Wallet.Domain.Accounts;
using Wallet.Domain.Common;

namespace Wallet.Api.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ISender _sender;

        public AccountsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken cancellationToken)
        {
            var account = await _sender.Send(new CreateAccountCommand(request.Amount, request.Currency), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account.ToResponse());
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

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AccountListResponse>>> GetAllMyAccounts(CancellationToken cancellationToken)
        {
            var accounts = await _sender.Send(new GetMyAccountsQuery(), cancellationToken);

            return Ok(accounts.Select(account => account.ToListResponse()).ToList());
        }
    }
}