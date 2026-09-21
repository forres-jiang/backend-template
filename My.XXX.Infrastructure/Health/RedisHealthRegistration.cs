using Microsoft.Extensions.DependencyInjection;
using System;

namespace My.XXX.Infrastructure;

public static class RedisHealthRegistration
{
    public static IServiceCollection AddRedisHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<Health.RedisHealthCheck>("redis",
            tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5));
        return services;
    }
}
