using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Languag.io.Application.ActivityLogs;
using Languag.io.Application.Decks;
using Languag.io.Application.Feed;
using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Application.StudySessions;
using Languag.io.Application.Users;
using Languag.io.Infrastructure.Repositories;
using Languag.io.Infrastructure.Users;

namespace Languag.io.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<IFeedRepository, FeedRepository>();
        services.AddScoped<IStudySessionRepository, StudySessionRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        // later: repositories, external services etc.
        return services;
    }
}
