namespace Wallet.Application.Abstractions.Exceptions
{
    public class DisplayNameTakenException : Exception
    {
        public DisplayNameTakenException(string displayName)
            : base($"'{displayName}' is already used by someone in your groups. Pick a name that tells people apart.")
        {
        }
    }
}
