using Languag.io.Application.Decks;
using Languag.io.Application.Feed;
using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Application.StudySessions;
using Languag.io.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Languag.io.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeckService, DeckService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IStudySessionService, StudySessionService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        // later: other services, mediators etc.
        return services;
    }
}
