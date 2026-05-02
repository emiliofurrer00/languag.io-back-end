using Languag.io.Application;
using Languag.io.Infrastructure;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AiDeckGenerationWorker>();

var host = builder.Build();

var applyMigrationsOnStartup =
    builder.Configuration.GetValue<bool?>("ApplyMigrationsOnStartup")
    ?? builder.Environment.IsDevelopment();

if (applyMigrationsOnStartup)
{
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
