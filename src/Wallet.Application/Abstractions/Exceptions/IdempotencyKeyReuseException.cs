namespace Wallet.Application.Abstractions.Exceptions
{
    public class IdempotencyKeyReuseException : Exception
    {
        public IdempotencyKeyReuseException(string key, string originalRequestName, string attemptedRequestName)
            : base($"Idempotency key '{key}' was already used for {originalRequestName} and cannot be reused for {attemptedRequestName}.")
        {
        }
    }
}
