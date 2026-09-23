using My.XXX.Contracts.DTOs;
using My.XXX.Services.Authentication.Interfaces;
using System;

namespace My.XXX.Services.Authentication
{
    public class UserService : IUserService
    {
        private readonly IAuthenticationSession _currentRequest;

        public UserService(IAuthenticationSession currentRequest)
        {
            _currentRequest = currentRequest;
        }

        public UserInfo CurrentUser => _currentRequest.User is { } user ? new UserInfo
        {
            UserId = user.UserId, UserName = user.UserName, Email = user.Email,
            Roles = new(user.Roles), RoleIds = new(user.RoleIds), Menus = new()
        } : null;
        public DateTime GetTokenExpirationTime() => _currentRequest.TokenExpirationTime;
    }
}
