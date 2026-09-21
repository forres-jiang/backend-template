using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using My.XXX.Services.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace My.XXX.APIs.Common;

public sealed class PermissionsRequirement(string policyName) : IAuthorizationRequirement
{
    public string Name { get; } = policyName;
}
public sealed class PermissionsHandler(IOptionsMonitor<PermissionWhitelist> whitelist,
    IPermissionQuery permissions, IUserService users) : AuthorizationHandler<PermissionsRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionsRequirement requirement)
    {
        if (context.Resource is not HttpContext http || context.User.Identity?.IsAuthenticated != true) return;
        var code = http.GetEndpoint()?.Metadata.GetMetadata<RequiresPermissionAttribute>()?.Code;
        // Missing metadata fails closed, even for administrators; works for MVC and minimal endpoints.
        if (string.IsNullOrWhiteSpace(code) || requirement.Name != PolicyType.Default) return;
        var user = users.CurrentUser;
        if (user == null) return;
        if (whitelist.CurrentValue.Codes?.Contains(code, StringComparer.Ordinal) == true ||
            user.Roles.Any(role => role.Equals("AppAdmin", StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }
        if (user.RoleIds == null || user.RoleIds.Count == 0) return;
        var codes = await permissions.GetRoleMenuPathsAsync(user.RoleIds, user.UserId, http.RequestAborted);
        if (codes.Contains(code, StringComparer.Ordinal)) context.Succeed(requirement);
    }
}
