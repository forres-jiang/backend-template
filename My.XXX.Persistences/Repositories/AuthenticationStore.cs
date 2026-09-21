using LinqToDB;
using LinqToDB.Async;
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Models;
using My.XXX.Services.Ports;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Persistences.Repositories;
public sealed class AuthenticationStore(DBContext db) : IAuthenticationStore
{
    public async Task SetUserAsync(UserInfo user, bool enabled, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(user.UserId);
        if (user.UserId.Length > 200) throw new ArgumentException("User ID is too long.");
        var authority = new UserInfo { UserId = user.UserId, UserName = user.UserName, Email = user.Email,
            Roles = user.Roles ?? new(), RoleIds = user.RoleIds ?? new() };
        await db.InsertOrReplaceAsync(new AuthenticationUser { UserId = user.UserId,
            UserJson = JsonConvert.SerializeObject(authority), Enabled = enabled }, token: cancellationToken);
    }
    public async Task<UserInfo> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var row = await db.GetTable<AuthenticationUser>().FirstOrDefaultAsync(u => u.UserId == userId && u.Enabled, cancellationToken);
        return row == null ? null : JsonConvert.DeserializeObject<UserInfo>(row.UserJson);
    }
    public async Task CreateSessionAsync(SessionState session, CancellationToken cancellationToken = default) =>
        await db.InsertAsync(new AuthenticationSessionRow { SessionId = session.SessionId, UserId = session.UserId,
            RefreshTokenId = session.RefreshTokenId, ExpiresUtc = DateTime.SpecifyKind(session.ExpiresUtc.ToUniversalTime(), DateTimeKind.Unspecified) }, token: cancellationToken);
    public async Task<ActiveSession> GetActiveSessionAsync(string sessionId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        nowUtc = DateTime.SpecifyKind(nowUtc.ToUniversalTime(), DateTimeKind.Unspecified);
        var row = await (from session in db.GetTable<AuthenticationSessionRow>()
                         join user in db.GetTable<AuthenticationUser>() on session.UserId equals user.UserId
                         where session.SessionId == sessionId && !session.Revoked && session.ExpiresUtc > nowUtc && user.Enabled
                         select new { Session = session, user.UserJson }).FirstOrDefaultAsync(cancellationToken);
        return row == null ? null : new ActiveSession { User = JsonConvert.DeserializeObject<UserInfo>(row.UserJson),
            Session = new SessionState { SessionId = row.Session.SessionId, UserId = row.Session.UserId,
                RefreshTokenId = row.Session.RefreshTokenId, ExpiresUtc = DateTime.SpecifyKind(row.Session.ExpiresUtc, DateTimeKind.Utc) } };
    }
    public async Task<bool> RotateAsync(string sessionId, string expectedTokenId, string nextTokenId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        nowUtc = DateTime.SpecifyKind(nowUtc.ToUniversalTime(), DateTimeKind.Unspecified);
        return await db.GetTable<AuthenticationSessionRow>()
            .Where(s => s.SessionId == sessionId && s.RefreshTokenId == expectedTokenId && !s.Revoked && s.ExpiresUtc > nowUtc)
            .Set(s => s.RefreshTokenId, nextTokenId).UpdateAsync(cancellationToken) == 1;
    }
    public async Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default) =>
        await db.GetTable<AuthenticationSessionRow>().Where(s => s.SessionId == sessionId)
            .Set(s => s.Revoked, true).UpdateAsync(cancellationToken);
}
