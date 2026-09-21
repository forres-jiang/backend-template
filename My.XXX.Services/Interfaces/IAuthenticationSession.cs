using System;

namespace My.XXX.Services.Interfaces;

public interface IAuthenticationSession : ICurrentUser
{
    bool IsAuthenticated { get; }
    DateTime TokenExpirationTime { get; }
    string SessionId { get; }
    string TokenId { get; }
}
