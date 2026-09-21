using My.XXX.Contracts.DTOs;
using My.XXX.Services.Models;
using My.XXX.Services.Ports;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.IntegrationTests;

internal sealed class InMemoryAuthenticationStore : IAuthenticationStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, (UserInfo User, bool Enabled)> users = new();
    private readonly Dictionary<string, SessionState> sessions = new();
    private static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    public Task SetUserAsync(UserInfo user, bool enabled, CancellationToken cancellationToken = default)
    { lock (gate) users[user.UserId] = (Copy(user), enabled); return Task.CompletedTask; }
    public Task<UserInfo> GetUserAsync(string id, CancellationToken cancellationToken = default)
    { lock (gate) return Task.FromResult(users.TryGetValue(id, out var u) && u.Enabled ? Copy(u.User) : null); }
    public Task CreateSessionAsync(SessionState session, CancellationToken cancellationToken = default)
    { lock (gate) sessions.Add(session.SessionId, Copy(session)); return Task.CompletedTask; }
    public Task<ActiveSession> GetActiveSessionAsync(string id, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        lock (gate) return Task.FromResult(sessions.TryGetValue(id, out var s) && s.ExpiresUtc > nowUtc &&
            users.TryGetValue(s.UserId, out var u) && u.Enabled
            ? new ActiveSession { Session = Copy(s), User = Copy(u.User) } : null);
    }
    public Task<bool> RotateAsync(string id, string expected, string next, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(id, out var s) || s.ExpiresUtc <= nowUtc || s.RefreshTokenId != expected) return Task.FromResult(false);
            s.RefreshTokenId = next;
            return Task.FromResult(true);
        }
    }
    public Task RevokeAsync(string id, CancellationToken cancellationToken = default)
    { lock (gate) sessions.Remove(id); return Task.CompletedTask; }
}
