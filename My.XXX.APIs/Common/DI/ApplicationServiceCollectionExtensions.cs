using Microsoft.Extensions.DependencyInjection;
using My.XXX.APIs.Common.JWT;
using My.XXX.Infrastructure;
using My.XXX.Persistences;
using My.XXX.Services;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.Authentication.Interfaces;
namespace My.XXX.APIs.Common.DI;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddBusinessServices();
        services.AddRepositories();
        services.AddExternalAdapters();
        services.AddScoped<IWebHelper, WebHelper>();
        services.AddScoped<HttpCurrentRequest>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpCurrentRequest>());
        services.AddScoped<ICurrentCulture>(sp => sp.GetRequiredService<HttpCurrentRequest>());
        services.AddScoped<IAuthenticationSession>(sp => sp.GetRequiredService<HttpCurrentRequest>());
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        return services;
    }
}
