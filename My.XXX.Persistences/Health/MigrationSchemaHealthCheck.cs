using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.XXX.Persistences.Migrations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Persistences.Health;

public sealed class MigrationSchemaHealthCheck(string connectionString, DatabaseProvider provider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await MigrationRunner.VerifyAsync(connectionString, provider, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Required schema migrations are missing or changed. Run the deployment migration command.");
        }
    }
}
