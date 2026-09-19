using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace My.XXX.APIs.Common;

public sealed class HttpCurrentRequest : ICurrentRequest
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<HttpCurrentRequest> _logger;

    public HttpCurrentRequest(IHttpContextAccessor accessor, ILogger<HttpCurrentRequest> logger)
    {
        _accessor = accessor;
        _logger = logger;
    }

    public ClaimsPrincipal Principal => _accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public UserInfo User
    {
        get
        {
            var principal = Principal;
            if (principal.Identity?.IsAuthenticated != true) return null;
            var result = new UserInfo
            {
                UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = principal.FindFirstValue(ClaimTypes.Name),
                Email = principal.FindFirstValue(ClaimTypes.Email),
                Roles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList(),
                RoleIds = new List<Guid>(),
                Menus = new List<MenuDto>()
            };
            var userData = principal.FindFirstValue(ClaimTypes.UserData);
            if (!string.IsNullOrWhiteSpace(userData))
            {
                try { result.RoleIds = JsonConvert.DeserializeObject<UserData>(userData)?.RoleIds ?? new List<Guid>(); }
                catch (JsonException ex) { _logger.LogWarning(ex, "Invalid user-data claim."); }
            }
            return result;
        }
    }

    public DateTime TokenExpirationTime =>
        long.TryParse(Principal.FindFirstValue("exp"), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime
            : DateTime.MinValue;

    public string CultureName =>
        _accessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name
        ?? CultureInfo.CurrentCulture.Name;
}
