using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using My.XXX.Services.Interfaces;
using My.XXX.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common
{
    public class Permissions : IAsyncAuthorizationFilter
    {
        private readonly PermissionWhitelist _permissionWhitelist;
        private readonly IPermissionQuery _permissions;
        private readonly IUserService _userService;

        public Permissions(
            IOptionsMonitor<PermissionWhitelist> permissionWhitelist,
            IPermissionQuery permissions,
            IUserService userService)
        {
            _permissionWhitelist = permissionWhitelist.CurrentValue;
            _permissions = permissions;
            _userService = userService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor actionObject)
            {
                var cwl = _permissionWhitelist.Controllers
                    .Where(m => m.Equals(actionObject.ControllerName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (cwl != null)
                {
                    return;
                }

                var path = $"{actionObject.ControllerName}/{actionObject.ActionName}";
                var action = _permissionWhitelist.Actions
                    .Where(m => m.Equals(path, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (action != null)
                {
                    return;
                }
            }

            if (IsIgnore(context.ActionDescriptor))
            {
                return;
            }

            if (!await IsPermissionAsync(context))
            {
                context.Result = new ForbidResult();
            }
        }

        private bool IsIgnore(ActionDescriptor context)
        {
            var emList = context.EndpointMetadata.ToList();
            string attributeName = typeof(IgnorePermission).ToString();
            bool IsVerify = false;

            foreach (var item in emList)
            {
                if (item.ToString().Equals(attributeName))
                {
                    IsVerify = true;
                    break;
                }
            }
            return IsVerify;
        }

        private async Task<bool> IsPermissionAsync(AuthorizationFilterContext context)
        {
            var user = _userService.CurrentUser;
            if (user == null)
            {
                return false;
            }

            //超级管理员不做权限校验
            var appAdmin = user.Roles.FirstOrDefault(m => m.Equals("AppAdmin", StringComparison.OrdinalIgnoreCase));
            if (null != appAdmin)
            {
                return true;
            }

            var paths = await _permissions.GetRoleMenuPathsAsync(user.RoleIds, user.UserId, context.HttpContext.RequestAborted);
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var currentPath = descriptor.ControllerName + "/" + descriptor.ActionName;
            var result = paths
                .Where(m => m.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            return result != null;
        }
    }

    public class IgnorePermission : ActionFilterAttribute
    { }
}