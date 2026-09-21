using My.XXX.Contracts.DTOs;
using System;

namespace My.XXX.Services.Authentication.Interfaces
{
    public interface IUserService
    {
        public UserInfo CurrentUser { get; }

        public DateTime GetTokenExpirationTime();
    }
}