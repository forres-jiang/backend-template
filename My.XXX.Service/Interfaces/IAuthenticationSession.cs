using System;

namespace My.XXX.Service.Interfaces;

public interface IAuthenticationSession : ICurrentUser
{
    bool IsAuthenticated { get; }
    DateTime TokenExpirationTime { get; }
}
