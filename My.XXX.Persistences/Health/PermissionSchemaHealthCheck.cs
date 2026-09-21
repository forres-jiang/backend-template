using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Persistences.Health;

public sealed class PermissionSchemaHealthCheck(IPermissionStore repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.GetPermissionRevisionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Permission schema unavailable. Apply the PermissionRevision database upgrade.");
        }
    }
}
