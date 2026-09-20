using Microsoft.Extensions.DependencyInjection;
using My.XXX.APIs.Common.JWT;
using My.XXX.Infrastructure;
using My.XXX.Persistence;
using My.XXX.Service;
using My.XXX.Service.Interfaces;
namespace My.XXX.APIs.Common.DI;
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddBusinessServices();
        services.AddRepositories();
        services.AddExternalAdapters();
        services.AddScoped<IWebHelper, WebHelper>();
        services.AddScoped<ICurrentRequest, HttpCurrentRequest>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        return services;
    }
}
