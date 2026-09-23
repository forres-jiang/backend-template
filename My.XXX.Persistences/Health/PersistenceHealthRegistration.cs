using Microsoft.Extensions.DependencyInjection;
using System;

namespace My.XXX.Persistences;

public static class PersistenceHealthRegistration
{
    public static IServiceCollection AddPersistenceHealthChecks(this IServiceCollection services,
        string connectionString, DatabaseProvider provider)
    {
        services.AddHealthChecks()
            .AddCheck("database", new Health.SqlHealthCheck(connectionString, provider),
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5))
            .AddCheck("schema-version", new Health.MigrationSchemaHealthCheck(connectionString, provider),
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5))
            .AddCheck<Health.PermissionSchemaHealthCheck>("permission-schema",
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5))
            .AddCheck<Health.AuthenticationSchemaHealthCheck>("authentication-schema",
                tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5));
        return services;
    }
}
