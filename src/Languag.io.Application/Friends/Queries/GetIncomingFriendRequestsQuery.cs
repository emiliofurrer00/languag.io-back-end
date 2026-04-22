using Languag.io.Application.Common;

namespace Languag.io.Application.Friends;

public sealed record GetIncomingFriendRequestsQuery(
    TimelineCursor? Cursor = null,
    int PageSize = 20);
