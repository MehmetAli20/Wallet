using System;

namespace Wallet.Domain.Groups
{
    public enum GroupTokenKind
    {
        Invite = 1,
        PlaceholderClaim = 2
    }

    public sealed record IssuedGroupToken(
        GroupTokenKind Kind,
        Guid Id,
        string Token,
        DateTimeOffset ExpiresAt,
        int MaxUses)
    {
        public override string ToString() =>
            $"{nameof(IssuedGroupToken)} {{ Kind = {Kind}, Id = {Id}, ExpiresAt = {ExpiresAt}, MaxUses = {MaxUses}, Token = *** }}";
    }
}
