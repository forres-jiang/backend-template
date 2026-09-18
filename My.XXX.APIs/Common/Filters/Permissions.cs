using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using My.XXX.Infra;
using My.XXX.Service.Interfaces;
using System;
using System.Linq;

namespace My.XXX.APIs.Common
{
    public class Permissions : IAuthorizationFilter
    {
        private readonly PermissionWhitelist _permissionWhitelist;
        private readonly IMenuService _menuService;
        private readonly IUserService _userService;

        public Permissions(
            IOptionsMonitor<PermissionWhitelist> permissionWhitelist,
            IMenuService menuService,
            IUserService userService)
        {
            _permissionWhitelist = permissionWhitelist.CurrentValue;
            _menuService = menuService;
            _userService = userService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
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

            if (!IsPermission(context))
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

        private bool IsPermission(AuthorizationFilterContext context)
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

            var paths = _menuService.GetRoleMenuPaths(user.RoleIds, user.UserId);
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