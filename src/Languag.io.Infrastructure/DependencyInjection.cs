using Languag.io.Application.ActivityLogs;
using Languag.io.Application.AiDeckGeneration;
using Languag.io.Application.Decks;
using Languag.io.Application.Feed;
using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Application.StudySessions;
using Languag.io.Application.Users;
using Languag.io.Infrastructure.AiDeckGeneration;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Repositories;
using Languag.io.Infrastructure.Storage;
using Languag.io.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IAiDeckGenerationJobRepository, AiDeckGenerationJobRepository>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<AiDeckGenerationProcessor>();
        services.Configure<OpenAiDeckGeneratorOptions>(options =>
        {
            options.ApiKey = configuration["OPENAI_API_KEY"] ?? configuration["OpenAI:ApiKey"];
            options.Model = configuration["OPENAI_MODEL"]
                ?? configuration["OpenAI:Model"]
                ?? options.Model;
        });

        var aiProvider = configuration["AI_PROVIDER"]
            ?? configuration["Ai:Provider"]
            ?? "Mock";

        if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiDeckGenerator, OpenAiDeckGenerator>(client =>
            {
                client.BaseAddress = new Uri("https://api.openai.com/v1/");
                client.Timeout = TimeSpan.FromSeconds(90);
            });
        }
        else
        {
            services.AddScoped<IAiDeckGenerator, MockAiDeckGenerator>();
        }

        services.AddSingleton<S3ProfilePictureStorage>();
        services.AddSingleton<IProfilePictureStorage>(sp => sp.GetRequiredService<S3ProfilePictureStorage>());
        services.AddSingleton<IProfilePictureUrlBuilder>(sp => sp.GetRequiredService<S3ProfilePictureStorage>());
        return services;
    }
}
