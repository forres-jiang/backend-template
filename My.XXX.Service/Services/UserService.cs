using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System;
using System.Security.Claims;

namespace My.XXX.Service
{
    public class UserService : IUserService
    {
        private readonly ICurrentRequest _currentRequest;

        public UserService(ICurrentRequest currentRequest)
        {
            _currentRequest = currentRequest;
        }

        public ClaimsPrincipal GetContextUser() => _currentRequest.Principal;
        public UserInfo CurrentUser => _currentRequest.User;
        public DateTime GetTokenExpirationTime() => _currentRequest.TokenExpirationTime;
    }
}
