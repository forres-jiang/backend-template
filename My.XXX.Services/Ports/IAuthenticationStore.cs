using System;
using System.Threading;
using System.Threading.Tasks;
using My.XXX.Service.DTOs;
using My.XXX.Services.Models;
namespace My.XXX.Services.Ports;
/// <summary>Shared persistent authority. Only trusted identity integrations may update users.</summary>
public interface IAuthenticationStore
{
    Task SetUserAsync(UserInfo user, bool enabled, CancellationToken cancellationToken = default);
    Task<UserInfo> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task CreateSessionAsync(SessionState session, CancellationToken cancellationToken = default);
    Task<ActiveSession> GetActiveSessionAsync(string sessionId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<bool> RotateAsync(string sessionId, string expectedTokenId, string nextTokenId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default);
}
