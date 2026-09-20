using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.XXX.Service.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.APIs.Common.Health;
public sealed class PermissionSchemaHealthCheck(IMenuRepository repository) : IHealthCheck
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
