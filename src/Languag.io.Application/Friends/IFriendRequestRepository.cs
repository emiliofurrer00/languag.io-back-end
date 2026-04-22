using Languag.io.Application.Common;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.Friends;

public interface IFriendRequestRepository
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default);
    Task<FriendRequest?> GetByIdAsync(Guid requestId, CancellationToken ct = default);
    Task<FriendRequest?> GetPendingByPairAsync(Guid pairUser1Id, Guid pairUser2Id, CancellationToken ct = default);
    Task AddAsync(FriendRequest friendRequest, CancellationToken ct = default);
    Task<CursorPage<FriendRequestDto>> GetIncomingPendingAsync(
        Guid currentUserId,
        GetIncomingFriendRequestsQuery query,
        CancellationToken ct = default);
    Task<CursorPage<FriendRequestDto>> GetOutgoingPendingAsync(
        Guid currentUserId,
        GetOutgoingFriendRequestsQuery query,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
