using Languag.io.Application.Decks;
using Languag.io.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Languag.io.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeckService, DeckService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        // later: other services, mediators etc.
        return services;
    }
}
