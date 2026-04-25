using Languag.io.Application.Common;
using Languag.io.Application.Friends;
using Languag.io.Application.Users;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class FriendshipRepository : IFriendshipRepository
{
    private const int MaximumPageSize = 100;
    private readonly AppDbContext _dbContext;
    private readonly IProfilePictureUrlBuilder _profilePictureUrlBuilder;

    public FriendshipRepository(AppDbContext dbContext, IProfilePictureUrlBuilder profilePictureUrlBuilder)
    {
        _dbContext = dbContext;
        _profilePictureUrlBuilder = profilePictureUrlBuilder;
    }

    public Task<bool> ExistsAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default)
    {
        return _dbContext.Friendships
            .AsNoTracking()
            .AnyAsync(friendship => friendship.User1Id == user1Id && friendship.User2Id == user2Id, ct);
    }

    public Task<Friendship?> GetByPairAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default)
    {
        return _dbContext.Friendships
            .SingleOrDefaultAsync(friendship => friendship.User1Id == user1Id && friendship.User2Id == user2Id, ct);
    }

    public async Task AddAsync(Friendship friendship, CancellationToken ct = default)
    {
        await _dbContext.Friendships.AddAsync(friendship, ct);
    }

    public void Remove(Friendship friendship)
    {
        _dbContext.Friendships.Remove(friendship);
    }

    public async Task<CursorPage<FriendDto>> GetFriendsAsync(
        Guid currentUserId,
        GetFriendsQuery query,
        CancellationToken ct = default)
    {
        var asUser1 = _dbContext.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.User1Id == currentUserId)
            .Select(friendship => new
            {
                UserId = friendship.User2Id,
                friendship.CreatedAtUtc,
                Username = friendship.User2.Username,
                Name = friendship.User2.Name,
                ExternalId = friendship.User2.ExternalId,
                friendship.User2.ProfilePictureObjectKey
            });

        var asUser2 = _dbContext.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.User2Id == currentUserId)
            .Select(friendship => new
            {
                UserId = friendship.User1Id,
                friendship.CreatedAtUtc,
                Username = friendship.User1.Username,
                Name = friendship.User1.Name,
                ExternalId = friendship.User1.ExternalId,
                friendship.User1.ProfilePictureObjectKey
            });

        var combinedQuery = asUser1.Concat(asUser2);
        if (query.Cursor is { } pageCursor)
        {
            combinedQuery = combinedQuery.Where(friend =>
                friend.CreatedAtUtc < pageCursor.CreatedAtUtc
                || (friend.CreatedAtUtc == pageCursor.CreatedAtUtc
                    && friend.UserId.CompareTo(pageCursor.Id) < 0));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var items = await combinedQuery
            .OrderByDescending(friend => friend.CreatedAtUtc)
            .ThenByDescending(friend => friend.UserId)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var friends = items
            .Select(friend => new FriendDto(
                friend.UserId,
                friend.Username,
                BuildDisplayName(friend.Username, friend.Name, friend.ExternalId),
                _profilePictureUrlBuilder.BuildPublicUrl(friend.ProfilePictureObjectKey),
                friend.CreatedAtUtc))
            .ToArray();

        var nextCursor = hasMore
            ? new TimelineCursor(items[^1].CreatedAtUtc, items[^1].UserId).Encode()
            : null;

        return new CursorPage<FriendDto>(friends, nextCursor);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    private static string BuildDisplayName(string? username, string? name, string externalId)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return BuildAnonymousDisplayName(externalId);
    }

    private static string BuildAnonymousDisplayName(string externalId)
    {
        var normalizedExternalId = externalId.Trim();
        var suffix = string.IsNullOrWhiteSpace(normalizedExternalId)
            ? "guest"
            : normalizedExternalId[..Math.Min(normalizedExternalId.Length, 8)];

        return $"User {suffix}";
    }
}
