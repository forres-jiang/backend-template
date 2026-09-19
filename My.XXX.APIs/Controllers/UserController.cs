using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.APIs.Common.JWT;
using My.XXX.Infra;
using My.XXX.Infra.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IAppCenterService _appCenterService;
        private readonly ILogger<UserController> _logger;
        private readonly IMenuService _menuService;
        private readonly IUserService _userService;
        private readonly IPermissionCache _permissionCache;
        private readonly JwtConfig _jwtConfig;
        private readonly AppConfig _appConfig;

        public UserController(
            IOptionsMonitor<AppConfig> appConfig,
            IAppCenterService appCenterService,
            IOptionsMonitor<JwtConfig> config,
            ILogger<UserController> logger,
            IMenuService menuService,
            IUserService userService,
            IPermissionCache permissionCache)
        {
            _appCenterService = appCenterService;
            _appConfig = appConfig.CurrentValue;
            _jwtConfig = config.CurrentValue;
            _menuService = menuService;
            _userService = userService;
            _permissionCache = permissionCache;
            _logger = logger;
        }

        [HttpPost, AllowAnonymous]
        [Route("Login")]
        public async Task<LoginResult> Login(LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Ticket))
            {
                return LoginResult.Fail("Ticket invalid.");
            }

            Users user = await _appCenterService.GetUserByTicket(model.Ticket);
            if (null == user)
            {
                _logger.LogError("Ticket invalid.");
                return LoginResult.Fail("Ticket invalid.");
            }

            var roles = new List<string>();
            var menu = new List<MenuDto>();
            var roleIds = new List<Guid>();
            if (user.Roles != null && user.Roles.Count > 0)
            {
                roles = user.Roles.Select(m => m.RoleName).Distinct().ToList();
                roleIds = user.Roles.Select(m => m.RoleID).Distinct().ToList();
                //menu = _menuService.GetMenuByRoles(new RoleMenuQuery { RoleIds = roleIds });
            }

            var userInfo = new UserInfo()
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Email = user.EmailAddress,
                Roles = roles,
                RoleIds = roleIds,
                Menus = menu,
            };

            var tokens = JwtTokenBuilder.CreateTokens(_jwtConfig, userInfo);

            return LoginResult.Success(new LoginUser
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Roles = roles,
                Menus = menu
            }, tokens.AccessToken, tokens.RefreshToken, _jwtConfig.ExpiryInMinutes);
        }

        [HttpPost]
        [Route("Logout")]
        public BaseResult Logout()
        {
            if (_appConfig.PermissionDataCache == PermissionDataCache.Redis)
            {
                var currentUser = _userService.CurrentUser;
                _permissionCache.Remove(currentUser.UserId);
            }
            return BaseResult.Success();
        }

        [HttpPost]
        [Route("RefreshToken")]
        [Authorize(AuthenticationSchemes = "Refresh")]
        public MyResult RefreshToken()
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.StatusCode = 401;
                return LoginResult.Fail("Token invalid.");
            }

            var currentUser = _userService.CurrentUser;
            var exprieTime = _userService.GetTokenExpirationTime();
            var tokens = JwtTokenBuilder.RefreshTokens(_jwtConfig, currentUser, exprieTime);

            return LoginResult.RefreshSuccess(tokens.AccessToken, tokens.RefreshToken, _jwtConfig.ExpiryInMinutes);
        }

        [HttpGet("GetRoles")]
        public MyResult GetRoles()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return LoginResult.Fail("Token invalid.");
            }

            var currentUser = _userService.CurrentUser;
            return MyResult.Success(currentUser);
        }
    }
}
