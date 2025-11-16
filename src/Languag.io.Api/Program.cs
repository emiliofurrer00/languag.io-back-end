using Languag.io.Application;
using Languag.io.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adding services to container

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen( c =>
{
    c.SwaggerDoc("v1", new() { Title = "Languag.io API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Languag.io API v1");
    });
}

app.UseHttpsRedirection();

// later: app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();