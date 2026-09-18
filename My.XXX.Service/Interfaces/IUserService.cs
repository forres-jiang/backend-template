using My.XXX.Service.DTOs;
using System;
using System.Security.Claims;

namespace My.XXX.Service.Interfaces
{
    public interface IUserService
    {
        public UserInfo CurrentUser { get; }

        public DateTime GetTokenExpirationTime();

        public ClaimsPrincipal GetContextUser();
    }
}