using My.XXX.Contracts.DTOs;
using System;
using System.Security.Claims;

namespace My.XXX.Services.Interfaces
{
    public interface IUserService
    {
        public UserInfo CurrentUser { get; }

        public DateTime GetTokenExpirationTime();

        public ClaimsPrincipal GetContextUser();
    }
}