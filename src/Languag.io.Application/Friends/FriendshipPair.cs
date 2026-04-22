namespace Languag.io.Application.Friends;

public readonly record struct FriendshipPair(Guid User1Id, Guid User2Id)
{
    public static FriendshipPair Normalize(Guid leftUserId, Guid rightUserId)
    {
        return leftUserId.CompareTo(rightUserId) <= 0
            ? new FriendshipPair(leftUserId, rightUserId)
            : new FriendshipPair(rightUserId, leftUserId);
    }
}
