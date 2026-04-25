using Languag.io.Api.Auth;
using Languag.io.Api.Webhooks;
using Languag.io.Application;
using Languag.io.Infrastructure;
using Languag.io.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Threading.RateLimiting;

const long DefaultMaxRequestBodyBytes = 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);

// Adding services to container
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize =
        builder.Configuration.GetValue<long?>("RequestLimits:MaxBodyBytes")
        ?? DefaultMaxRequestBodyBytes;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("profile-image-upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("sub")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddSingleton<IKindeWebhookDecoder, KindeWebhookDecoder>();
builder.Services.Configure<KindeJwtOptions>(builder.Configuration.GetSection(KindeJwtOptions.SectionName));

var kindeOptions = builder.Configuration.GetSection(KindeJwtOptions.SectionName).Get<KindeJwtOptions>() ?? new KindeJwtOptions();

if (string.IsNullOrWhiteSpace(kindeOptions.Authority) || string.IsNullOrWhiteSpace(kindeOptions.Audience))
{
    throw new InvalidOperationException(
        "Kinde JWT authentication is missing configuration. Set Authentication:Kinde:Authority and Authentication:Kinde:Audience before starting the API.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = kindeOptions.Authority;
        options.Audience = kindeOptions.Audience;
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            NameClaimType = "name"
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KindeJwt");

                logger.LogError(context.Exception, "JWT authentication failed.");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KindeJwt");

                logger.LogWarning(
                    "JWT challenge triggered. Error={Error}",
                    context.Error);

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KindeJwt");

                var audiences = context.Principal?.FindAll("aud").Select(claim => claim.Value).ToArray() ?? [];

                logger.LogDebug(
                    "JWT validated. Audiences: {Audiences}",
                    string.Join(", ", audiences));

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Languag.io API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste a Kinde access token here."
    });
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", hostDocument: null!, externalResource: null),
            []
        }
    });
});
builder.Services.AddCors();
builder.Services.AddControllers();

var app = builder.Build();

var applyMigrationsOnStartup =
    builder.Configuration.GetValue<bool?>("ApplyMigrationsOnStartup")
    ?? app.Environment.IsDevelopment();

if (applyMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Languag.io API v1");
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(
    options => options.WithOrigins(ReadAllowedOrigins(builder.Configuration))
        .AllowAnyHeader()
        .AllowAnyMethod()
);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

static string[] ReadAllowedOrigins(IConfiguration configuration)
{
    var originsFromEnv = (configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(origin => origin.TrimEnd('/'))
        .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (originsFromEnv.Length > 0)
    {
        return originsFromEnv;
    }

    var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (configuredOrigins is { Length: > 0 })
    {
        var validConfiguredOrigins = configuredOrigins
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (validConfiguredOrigins.Length > 0)
        {
            return validConfiguredOrigins;
        }
    }

    return ["http://localhost:3000", "https://languagio.vercel.app"];
}
