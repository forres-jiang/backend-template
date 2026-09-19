using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.XXX.Persistence;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common.Health;

public sealed class SqlHealthCheck(string connectionString, DatabaseProvider provider = DatabaseProvider.SqlServer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = DatabaseConfiguration.CreateConnection(connectionString, provider);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 3;
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database unavailable.");
        }
    }
}

public sealed class RedisHealthCheck(IConnectionMultiplexer connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.GetDatabase().PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Redis unavailable.");
        }
    }
}
