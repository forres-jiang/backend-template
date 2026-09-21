using My.XXX.Services.Abstractions.Interfaces;
using System;

namespace My.XXX.Services.Authentication.Interfaces;

public interface IAuthenticationSession : ICurrentUser
{
    bool IsAuthenticated { get; }
    DateTime TokenExpirationTime { get; }
    string SessionId { get; }
    string TokenId { get; }
}
