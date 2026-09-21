using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.XXX.Services.Authentication.Ports;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Persistences.Health;

public sealed class AuthenticationSchemaHealthCheck(IAuthenticationStore sessions, IRolePermissionStore permissions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await sessions.GetActiveSessionAsync("health-schema-probe", DateTime.UtcNow, cancellationToken);
            await permissions.GetAsync(Guid.Empty, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Apply the authentication and stable permission schema upgrades.");
        }
    }
}
