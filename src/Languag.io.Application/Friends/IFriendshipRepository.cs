using Languag.io.Application.Common;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.Friends;

public interface IFriendshipRepository
{
    Task<bool> ExistsAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default);
    Task<Friendship?> GetByPairAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default);
    Task AddAsync(Friendship friendship, CancellationToken ct = default);
    void Remove(Friendship friendship);
    Task<CursorPage<FriendDto>> GetFriendsAsync(
        Guid currentUserId,
        GetFriendsQuery query,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
