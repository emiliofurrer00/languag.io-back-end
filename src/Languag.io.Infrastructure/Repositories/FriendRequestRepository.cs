using Languag.io.Application.Common;
using Languag.io.Application.Friends;
using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class FriendRequestRepository : IFriendRequestRepository
{
    private const int MaximumPageSize = 100;
    private readonly AppDbContext _dbContext;

    public FriendRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, ct);
    }

    public Task<FriendRequest?> GetByIdAsync(Guid requestId, CancellationToken ct = default)
    {
        return _dbContext.FriendRequests
            .SingleOrDefaultAsync(friendRequest => friendRequest.Id == requestId, ct);
    }

    public Task<FriendRequest?> GetPendingByPairAsync(Guid pairUser1Id, Guid pairUser2Id, CancellationToken ct = default)
    {
        return _dbContext.FriendRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                friendRequest => friendRequest.PairUser1Id == pairUser1Id
                    && friendRequest.PairUser2Id == pairUser2Id
                    && friendRequest.Status == FriendRequestStatus.Pending,
                ct);
    }

    public async Task AddAsync(FriendRequest friendRequest, CancellationToken ct = default)
    {
        await _dbContext.FriendRequests.AddAsync(friendRequest, ct);
    }

    public async Task<CursorPage<FriendRequestDto>> GetIncomingPendingAsync(
        Guid currentUserId,
        GetIncomingFriendRequestsQuery query,
        CancellationToken ct = default)
    {
        var entityQuery = _dbContext.FriendRequests
            .AsNoTracking()
            .Where(friendRequest => friendRequest.ReceiverId == currentUserId && friendRequest.Status == FriendRequestStatus.Pending);

        return await ReadPageAsync(entityQuery, query.Cursor, NormalizePageSize(query.PageSize), ct);
    }

    public async Task<CursorPage<FriendRequestDto>> GetOutgoingPendingAsync(
        Guid currentUserId,
        GetOutgoingFriendRequestsQuery query,
        CancellationToken ct = default)
    {
        var entityQuery = _dbContext.FriendRequests
            .AsNoTracking()
            .Where(friendRequest => friendRequest.SenderId == currentUserId && friendRequest.Status == FriendRequestStatus.Pending);

        return await ReadPageAsync(entityQuery, query.Cursor, NormalizePageSize(query.PageSize), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    private static int NormalizePageSize(int requestedPageSize)
    {
        return Math.Clamp(requestedPageSize, 1, MaximumPageSize);
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

    private async Task<CursorPage<FriendRequestDto>> ReadPageAsync(
        IQueryable<FriendRequest> query,
        TimelineCursor? cursor,
        int pageSize,
        CancellationToken ct)
    {
        if (cursor is { } pageCursor)
        {
            query = query.Where(friendRequest =>
                friendRequest.CreatedAtUtc < pageCursor.CreatedAtUtc
                || (friendRequest.CreatedAtUtc == pageCursor.CreatedAtUtc
                    && friendRequest.Id.CompareTo(pageCursor.Id) < 0));
        }

        var items = await query
            .OrderByDescending(friendRequest => friendRequest.CreatedAtUtc)
            .ThenByDescending(friendRequest => friendRequest.Id)
            .Select(friendRequest => new
            {
                friendRequest.Id,
                friendRequest.CreatedAtUtc,
                friendRequest.SenderId,
                SenderUsername = friendRequest.Sender.Username,
                SenderName = friendRequest.Sender.Name,
                SenderExternalId = friendRequest.Sender.ExternalId,
                friendRequest.ReceiverId,
                ReceiverUsername = friendRequest.Receiver.Username,
                ReceiverName = friendRequest.Receiver.Name,
                ReceiverExternalId = friendRequest.Receiver.ExternalId,
                friendRequest.Status
            })
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var dtos = items
            .Select(friendRequest => new FriendRequestDto(
                friendRequest.Id,
                friendRequest.SenderId,
                friendRequest.SenderUsername,
                BuildDisplayName(friendRequest.SenderUsername, friendRequest.SenderName, friendRequest.SenderExternalId),
                null,
                friendRequest.ReceiverId,
                friendRequest.ReceiverUsername,
                BuildDisplayName(friendRequest.ReceiverUsername, friendRequest.ReceiverName, friendRequest.ReceiverExternalId),
                null,
                friendRequest.Status,
                friendRequest.CreatedAtUtc))
            .ToArray();

        var nextCursor = hasMore
            ? new TimelineCursor(items[^1].CreatedAtUtc, items[^1].Id).Encode()
            : null;

        return new CursorPage<FriendRequestDto>(dtos, nextCursor);
    }
}
