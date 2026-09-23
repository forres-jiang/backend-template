using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using My.XXX.Infrastructure.Caching;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Operations.Interfaces;
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
        services.AddOptions<Logging.RequestLogOptions>();
        services.AddScoped<Logging.RequestLogWriter>();
        services.AddSingleton<Logging.QueuedRequestLogWriter>();
        services.AddSingleton<IRequestLogWriter>(sp => sp.GetRequiredService<Logging.QueuedRequestLogWriter>());
        services.AddHostedService(sp => sp.GetRequiredService<Logging.QueuedRequestLogWriter>());
        return services;
    }
    public static IServiceCollection AddPermissionCaching(this IServiceCollection services, string connectionString, bool enabled)
    {
        services.AddOptions<PermissionCacheOptions>();
        services.Replace(ServiceDescriptor.Scoped<IPermissionQuery, PermissionQuery>());
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
        services.AddScoped<PermissionCacheMaintenance>();
        if (enabled) services.Replace(ServiceDescriptor.Scoped<IPermissionQuery, CachedPermissionQuery>());
        return services;
    }
}
