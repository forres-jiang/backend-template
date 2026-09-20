using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using My.XXX.APIs.Common;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using System.Threading.Tasks;

namespace My.XXX.APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IAuthenticationService _authentication;
    private readonly IUserService _users;
    public UserController(IAuthenticationService authentication, IUserService users)
    {
        _authentication = authentication; _users = users;
    }

    [HttpPost]
    [Route("RefreshToken")]
    [Authorize(AuthenticationSchemes = "Refresh")]
    public async Task<LoginResult> RefreshToken()
    {
        var result = await _authentication.RefreshAsync(HttpContext.RequestAborted);
        if (result.IsFailed) Response.StatusCode = 401;
        return result.ToLoginResult();
    }

    [HttpDelete("Session")]
    public async Task<BaseResult> RevokeSession()
    {
        await _authentication.LogoutAsync(HttpContext.RequestAborted);
        return BaseResult.Success();
    }

    [HttpGet("GetRoles")]
    public MyResult GetRoles()
    {
        if (User.Identity?.IsAuthenticated != true) return LoginResult.Fail("Token invalid.");
        return MyResult.Success(_users.CurrentUser);
    }
}
