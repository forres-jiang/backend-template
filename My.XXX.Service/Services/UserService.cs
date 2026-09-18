using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace My.XXX.Service
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;

        public UserService(IHttpContextAccessor httpContextAccessor, ILogger<UserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public ClaimsPrincipal GetContextUser()
        {
            return _httpContextAccessor.HttpContext.User;
        }

        public UserInfo CurrentUser
        {
            get
            {
                var user = _httpContextAccessor.HttpContext.User;
                if (!user.Identity.IsAuthenticated)
                {
                    return null;
                }

                var userInfo = new UserInfo
                {
                    UserId = user.Claims.First(p => p.Type == ClaimTypes.NameIdentifier).Value,
                    UserName = user.Claims.First(p => p.Type == ClaimTypes.Name).Value,
                    Email = user.Claims.First(p => p.Type == ClaimTypes.Email).Value,
                    Roles = new List<string>(),
                    RoleIds = new List<Guid>(),
                    Menus = new List<MenuDto>()
                };

                var roles = user.FindAll(p => p.Type == ClaimTypes.Role).Select(m => m.Value).ToList();
                if (roles.Count > 0)
                {
                    userInfo.Roles.AddRange(roles);
                }

                var userDate = user.FindAll(p => p.Type == ClaimTypes.UserData).FirstOrDefault();
                if (null != userDate)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<UserData>(userDate.Value);
                        userInfo.RoleIds = data.RoleIds;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("{Message}", ex.Message);
                    }
                }

                return userInfo;
            }
        }

        public DateTime GetTokenExpirationTime()
        {
            var user = _httpContextAccessor.HttpContext.User;
            var exp = user.Claims.FirstOrDefault(m => m.Type == "exp");

            if (null == exp)
            {
                return DateTime.MinValue;
            }

            var dt = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Local);
            var longTime = long.Parse(exp.Value + "0000000");

            return dt.Add(new TimeSpan(longTime));
        }
    }
}