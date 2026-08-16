namespace Wallet.Application.Abstractions.Exceptions
{
    public class IdempotentResponseUnavailableException : Exception
    {
        public IdempotentResponseUnavailableException(string key)
            : base($"Request with idempotency key '{key}' was already processed, but its response was not recorded.")
        {
        }
    }
}
