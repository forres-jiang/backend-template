using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common
{
    public class PermissionsRequirement : IAuthorizationRequirement
    {
        public PermissionsRequirement(string policyName)
        {
            Name = policyName;
        }

        public string Name { get; set; }
    }

    public class PermissionsHandler : AuthorizationHandler<PermissionsRequirement>
    {
        private readonly PermissionWhitelist _permissionWhitelist;
        private readonly IPermissionQuery _permissions;
        private readonly IUserService _userService;

        public PermissionsHandler(
            IOptionsMonitor<PermissionWhitelist> permissionWhitelist,
            IPermissionQuery permissions,
            IUserService userService)
        {
            _permissionWhitelist = permissionWhitelist.CurrentValue;
            _permissions = permissions;
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionsRequirement requirement)
        {
            var user = _userService.CurrentUser;
            //判断是否登录
            if (user is null)
            {
                return;
            }

            //判断是否配置角色
            if (user.Roles.Count == 0)
            {
                return;
            }

            string requestPath = string.Empty;
            ControllerActionDescriptor descriptor = null;
            if (context.Resource is HttpContext httpContext)
            {
                var endpoint = httpContext.GetEndpoint();
                descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                requestPath = $"{descriptor.ControllerName}/{descriptor.ActionName}";
            }
            else
            {
                return;
            }

            if (_permissionWhitelist != null && _permissionWhitelist.Controllers != null)
            {
                var isController = _permissionWhitelist.Controllers
                    .Where(m => m.Equals(descriptor.ControllerName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (isController != null)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            if (_permissionWhitelist != null && _permissionWhitelist.Actions != null)
            {
                var isAction = _permissionWhitelist.Actions
                .Where(m => m.Equals(requestPath, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

                if (isAction != null)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            //超级管理员不做权限校验
            var appAdmin = user.Roles.FirstOrDefault(m => m.Equals("AppAdmin", StringComparison.OrdinalIgnoreCase));
            if (null != appAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            bool flag = false;
            var paths = await _permissions.GetRoleMenuPathsAsync(user.RoleIds, user.UserId, httpContext.RequestAborted);

            if (requirement.Name == PolicyType.Default)
            {
                var result = paths.FirstOrDefault(m => m.Equals(requestPath, StringComparison.OrdinalIgnoreCase));
                if (null == result)
                {
                    return;
                }

                flag = true;
            }

            if (flag)
            {
                //验证通过
                context.Succeed(requirement);
            }
            return;
        }
    }
}