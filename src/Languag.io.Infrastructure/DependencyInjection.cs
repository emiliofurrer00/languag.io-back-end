using Languag.io.Application.ActivityLogs;
using Languag.io.Application.AiDeckGeneration;
using Languag.io.Application.Audio;
using Languag.io.Application.Decks;
using Languag.io.Application.Feed;
using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Application.Sagas;
using Languag.io.Application.StudySessions;
using Languag.io.Application.Users;
using Languag.io.Infrastructure.Audio;
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
        services.AddScoped<ISagaRepository, SagaRepository>();
        services.AddScoped<IStudySessionRepository, StudySessionRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAiDeckGenerationJobRepository, AiDeckGenerationJobRepository>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IAudioAssetService, AudioAssetService>();
        services.AddScoped<AiDeckGenerationProcessor>();
        services.Configure<OpenAiDeckGeneratorOptions>(options =>
        {
            options.ApiKey = configuration["OPENAI_API_KEY"] ?? configuration["OpenAI:ApiKey"];
            options.Model = configuration["OPENAI_MODEL"]
                ?? configuration["OpenAI:Model"]
                ?? options.Model;
        });

        services.Configure<OpenAiTextToSpeechOptions>(options =>
        {
            options.ApiKey = configuration["OPENAI_API_KEY"] ?? configuration["OpenAI:ApiKey"] ?? string.Empty;
            options.Model = configuration["OPENAI_TTS_MODEL"]
                ?? configuration["OpenAI:TtsModel"]
                ?? options.Model;
            options.Voice = configuration["OPENAI_TTS_VOICE"]
                ?? configuration["OpenAI:TtsVoice"]
                ?? options.Voice;
            options.InstructionsVersion = configuration["OPENAI_TTS_INSTRUCTIONS_VERSION"]
                ?? configuration["OpenAI:TtsInstructionsVersion"]
                ?? options.InstructionsVersion;

            var speedValue = configuration["OPENAI_TTS_SPEED"] ?? configuration["OpenAI:TtsSpeed"];
            if (decimal.TryParse(speedValue, out var speed) && speed is >= 0.25m and <= 4m)
            {
                options.Speed = speed;
            }
        });

        services.Configure<AudioStorageOptions>(options =>
        {
            options.AccessKeyId = configuration["AudioStorage:AccessKeyId"]
                ?? configuration["AWS_ACCESS_KEY_ID"]
                ?? string.Empty;
            options.SecretAccessKey = configuration["AudioStorage:SecretAccessKey"]
                ?? configuration["AWS_SECRET_ACCESS_KEY"]
                ?? string.Empty;
            options.SessionToken = configuration["AudioStorage:SessionToken"]
                ?? configuration["AWS_SESSION_TOKEN"];
            options.BucketName = configuration["AudioStorage:BucketName"]
                ?? configuration["AWS_S3_BUCKET"]
                ?? configuration["AWS_BUCKET_NAME"]
                ?? string.Empty;
            options.Region = configuration["AudioStorage:Region"]
                ?? configuration["AWS_REGION"]
                ?? options.Region;
            options.ServiceUrl = configuration["AudioStorage:ServiceUrl"]
                ?? configuration["AWS_S3_SERVICE_URL"];
            options.PublicBaseUrl = configuration["AudioStorage:PublicBaseUrl"]
                ?? configuration["AUDIO_CDN_BASE_URL"]
                ?? configuration["CLOUDFRONT_BASE_URL"]
                ?? string.Empty;
            options.KeyPrefix = configuration["AudioStorage:KeyPrefix"]
                ?? options.KeyPrefix;
        });

        services.AddHttpClient<ITextToSpeechService, OpenAiTextToSpeechService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddSingleton<IAudioStorageService, S3AudioStorageService>();

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
