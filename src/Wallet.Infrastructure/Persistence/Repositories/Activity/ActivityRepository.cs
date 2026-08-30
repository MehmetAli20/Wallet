using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Activity;
using Wallet.Application.Abstractions.Users;
using Wallet.Domain.Activity;

namespace Wallet.Infrastructure.Persistence.Repositories.Activity
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly WalletDbContext _context;
        private readonly ICurrentUser _currentUser;

        public ActivityRepository(WalletDbContext context, ICurrentUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ActivityEntry>> GetForGroupAsync(
            Guid groupId, long? after, int limit, CancellationToken cancellationToken = default)
        {
            var query = _context.ActivityEntries.Where(a => a.GroupId == groupId);

            if (after is not null)
                query = query.Where(a => a.Sequence > after.Value);

            return await query
                .OrderByDescending(a => a.Sequence)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<GroupActivityRead?> GetReadCursorAsync(
            Guid groupId, CancellationToken cancellationToken = default) =>
            await _context.GroupActivityReads
                .FirstOrDefaultAsync(r => r.GroupId == groupId, cancellationToken);

        public async Task AddReadCursorAsync(
            GroupActivityRead cursor, CancellationToken cancellationToken = default) =>
            await _context.GroupActivityReads.AddAsync(cursor, cancellationToken);

        public async Task<IReadOnlyList<GroupUnreadCount>> GetUnreadCountsAsync(
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;

            var query =
                from entry in _context.ActivityEntries
                where entry.ActorId != userId
                join cursor in _context.GroupActivityReads
                    on entry.GroupId equals cursor.GroupId into cursors
                from cursor in cursors.DefaultIfEmpty()
                where cursor == null || entry.Sequence > cursor.LastSeenSequence
                group entry by entry.GroupId into grouped
                select new GroupUnreadCount(grouped.Key, grouped.Count());

            return await query.ToListAsync(cancellationToken);
        }
    }
}
