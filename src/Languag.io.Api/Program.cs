using Languag.io.Application;
using Languag.io.Infrastructure;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Adding services to container

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen( c =>
{
    c.SwaggerDoc("v1", new() { Title = "Languag.io API", Version = "v1" });
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
    options => options.WithOrigins(["http://localhost:3000", "https://languagio.vercel.app"]).AllowAnyHeader().AllowAnyMethod()
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

// later: app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();