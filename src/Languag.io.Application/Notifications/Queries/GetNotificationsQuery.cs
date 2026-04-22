using Languag.io.Application.Common;

namespace Languag.io.Application.Notifications;

public sealed record GetNotificationsQuery(
    TimelineCursor? Cursor = null,
    int PageSize = 20);
