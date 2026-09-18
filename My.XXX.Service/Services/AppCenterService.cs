using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Infra;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace My.XXX.Service
{
    public class AppCenterService : IAppCenterService, IScopeDependency
    {
        private const string CacheKey = "token";
        private readonly ILogger<AppCenterService> _logger;
        private readonly AppCenterConfig _appCenterConfig;
        private readonly IHttpService _httpService;
        private readonly IMemoryCache _cache;

        public AppCenterService(
            IOptionsMonitor<AppCenterConfig> appCenterConfig,
            ILogger<AppCenterService> logger,
            IHttpService httpService,
            IMemoryCache cache)
        {
            _appCenterConfig = appCenterConfig.CurrentValue;
            _httpService = httpService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> GetToken()
        {
            var cache = _cache.Get<string>(CacheKey);
            if (!string.IsNullOrEmpty(cache))
            {
                return cache;
            }

            var result = await _httpService.Post<string>(new RequestModel()
            {
                Url = _appCenterConfig.UserInfoAPI + "/App/GetToken",
                Parameters = new { _appCenterConfig.AppCode, _appCenterConfig.AppSecret }
            });

            var tokenExpireTime = _appCenterConfig?.TokenExpireTime ?? 1;
            _cache.Set(CacheKey, result?.Data, TimeSpan.FromHours(tokenExpireTime * 24 - 1));
            return result?.Data;
        }

        public async Task<string> GetBearerToken()
        {
            return $"Bearer {await GetToken()}";
        }

        public async Task<Users> GetUserByTicket(string ticket)
        {
            var result = await _httpService.Post<Users>(new RequestModel
            {
                Token = await GetBearerToken(),
                Url = _appCenterConfig.UserInfoAPI + "/User/GetUserInfoByTicket",
                Parameters = new { Ticket = ticket }
            });

            if (result.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                _cache.Remove(CacheKey);
                _logger.LogError("API token invalid.");
                return null;
            }

            if (result.Status != 1)
            {
                _logger.LogError("Ticket error:{error}", result.Message);
                return null;
            }

            return result.Data;
        }

        public async Task<Users> GetUserById(string UserId)
        {
            var result = await _httpService.Post<Users>(new RequestModel
            {
                Token = await GetBearerToken(),
                Url = _appCenterConfig.UserInfoAPI + "/User/GetUserById",
                Parameters = new { UserId }
            });

            return result.Data;
        }

        public async Task<List<Role>> GetRole()
        {
            var result = await _httpService.Post<List<Role>>(new RequestModel
            {
                Token = await GetBearerToken(),
                Url = _appCenterConfig.UserInfoAPI + "/App/GetRoles",
            });

            return result.Data;
        }
    }
}