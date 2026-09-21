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

        public UserInfo CurrentUser => _currentRequest.User;
        public DateTime GetTokenExpirationTime() => _currentRequest.TokenExpirationTime;
    }
}
