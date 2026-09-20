using Microsoft.Extensions.DependencyInjection;
using My.XXX.Service.Interfaces;
using StackExchange.Redis;
using System;
namespace My.XXX.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddExternalAdapters(this IServiceCollection services)
    {
        services.AddScoped<IHttpService, HttpService>();
        services.AddHttpClient("External", client => client.Timeout = TimeSpan.FromSeconds(30)).RemoveAllLoggers();
        services.AddMemoryCache();
        return services;
    }
    public static IServiceCollection AddPermissionCaching(this IServiceCollection services, string connectionString, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (enabled) throw new InvalidOperationException("Redis permission caching requires RedisConfig:ConnectionString.");
            services.AddSingleton<IPermissionCache, NullPermissionCache>();
            return services;
        }
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.BacklogPolicy = BacklogPolicy.FailFast;
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
        services.AddSingleton<IPermissionCache, PermissionCache>();
        return services;
    }
}
