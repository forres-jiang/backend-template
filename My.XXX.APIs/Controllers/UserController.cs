using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using My.XXX.APIs.Common;
using My.XXX.Service.DTOs;
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

    [HttpPost, AllowAnonymous]
    [Route("Login")]
    public async Task<LoginResult> Login(LoginModel model) =>
        (await _authentication.Login(model.Ticket)).ToLoginResult();

    [HttpPost]
    [Route("Logout")]
    public BaseResult Logout()
    {
        _authentication.Logout();
        return BaseResult.Success();
    }

    [HttpPost]
    [Route("RefreshToken")]
    [Authorize(AuthenticationSchemes = "Refresh")]
    public MyResult RefreshToken()
    {
        var result = _authentication.Refresh();
        if (result.IsFailed) Response.StatusCode = 401;
        return result.ToLoginResult();
    }

    [HttpGet("GetRoles")]
    public MyResult GetRoles()
    {
        if (User.Identity?.IsAuthenticated != true) return LoginResult.Fail("Token invalid.");
        return MyResult.Success(_users.CurrentUser);
    }
}
