using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using My.XXX.APIs.Configurations;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.Authorization.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace My.XXX.APIs.Common;

public sealed class PermissionsRequirement(string policyName) : IAuthorizationRequirement
{
    public string Name { get; } = policyName;
}
public sealed class PermissionsHandler(IOptionsMonitor<PermissionWhitelist> whitelist,
    IPermissionQuery permissions, ICurrentUser users) : AuthorizationHandler<PermissionsRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionsRequirement requirement)
    {
        if (context.Resource is not HttpContext http || context.User.Identity?.IsAuthenticated != true) return;
        var code = http.GetEndpoint()?.Metadata.GetMetadata<RequiresPermissionAttribute>()?.Code;
        // 缺失元数据时采用失败关闭（fail closed）策略，即使是管理员也不例外；对 MVC 和 Minimal API 端点均适用。
        if (string.IsNullOrWhiteSpace(code) || requirement.Name != PolicyType.Default) return;
        var user = users.User;
        if (user == null) return;
        if (whitelist.CurrentValue.Codes?.Contains(code, StringComparer.Ordinal) == true ||
            user.Roles.Any(role => role.Equals("AppAdmin", StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }
        if (user.RoleIds == null || user.RoleIds.Count == 0) return;
        var codes = await permissions.GetPermissionCodesAsync(user.RoleIds.ToList(), user.UserId, http.RequestAborted);
        if (codes.Contains(code, StringComparer.Ordinal)) context.Succeed(requirement);
    }
}
