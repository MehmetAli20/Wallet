namespace Wallet.Application.Abstractions.Exceptions
{
    public class IdempotencyPayloadMismatchException : Exception
    {
        public IdempotencyPayloadMismatchException(string key)
            : base($"Idempotency key '{key}' was already used with a different request body.")
        {
        }
    }
}
