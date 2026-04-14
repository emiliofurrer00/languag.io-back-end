using Languag.io.Api.Auth;
using Languag.io.Api.Webhooks;
using Languag.io.Application;
using Languag.io.Infrastructure;
using Languag.io.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Adding services to container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
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
        options.IncludeErrorDetails = true; //builder.Environment.IsDevelopment();
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
                    "JWT challenge triggered. Error={Error}, Description={Description}",
                    context.Error,
                    context.ErrorDescription);

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("KindeJwt");

                var subject = context.Principal?.FindFirst("sub")?.Value;
                var audiences = context.Principal?.FindAll("aud").Select(claim => claim.Value).ToArray() ?? [];

                logger.LogInformation(
                    "JWT validated for subject {Subject}. Audiences: {Audiences}",
                    subject,
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

// Run migrations at startup
// Might want to gate this with an env var later
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors(
    options => options.WithOrigins(["http://localhost:3000", "https://languagio.vercel.app", "52.215.16.239", "54.216.8.72", "63.33.109.123", "2a05:d028:17:8000::/56"]).AllowAnyHeader().AllowAnyMethod()
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Languag.io API v1");
    });
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
