using System;
using My.XXX.Service.DTOs;

namespace My.XXX.Services.Models;
public sealed class SessionState
{
    public string SessionId { get; set; }
    public string UserId { get; set; }
    public string RefreshTokenId { get; set; }
    public DateTime ExpiresUtc { get; set; }
}
public sealed class ActiveSession
{
    public SessionState Session { get; set; }
    public UserInfo User { get; set; }
}
