using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Models;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.APIs.Common.JWT;

public sealed class JwtTokenIssuer(IOptions<JwtConfig> options, IAuthenticationStore store, TimeProvider clock) : ITokenIssuer
{
    public async Task<TokenPair> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("The user is not enabled in the identity authority.");
        var session = new SessionState { SessionId = Guid.NewGuid().ToString("N"), UserId = userId,
            RefreshTokenId = Guid.NewGuid().ToString("N"), ExpiresUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(options.Value.RefreshExpiryInMinutes) };
        var tokens = Create(user, session.SessionId, session.RefreshTokenId, session.ExpiresUtc);
        await store.CreateSessionAsync(session, cancellationToken);
        return tokens;
    }
    public TokenPair Create(UserInfo user, string sessionId, string refreshTokenId, DateTime refreshExpiration)
    {
        var config = options.Value;
        var accessExpiration = clock.GetUtcNow().UtcDateTime.AddMinutes(config.ExpiryInMinutes);
        if (accessExpiration > refreshExpiration) accessExpiration = refreshExpiration;
        string Build(string purpose, string id, DateTime expiration)
        {
            var claims = new List<Claim> { new("sid", sessionId), new("jti", id), new("token_use", purpose),
                new(ClaimTypes.NameIdentifier, user.UserId), new(ClaimTypes.Name, user.UserName ?? ""),
                new(ClaimTypes.Email, user.Email ?? ""), new(ClaimTypes.UserData, JsonConvert.SerializeObject(new UserData { RoleIds = user.RoleIds ?? new() })) };
            foreach (var role in user.Roles ?? new()) claims.Add(new Claim(ClaimTypes.Role, role));
            var token = new JwtSecurityToken(config.Issuer, config.Audience + (purpose == "refresh" ? ":refresh" : ""),
                claims, expires: expiration, signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Secret)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        return new TokenPair { AccessToken = Build("access", Guid.NewGuid().ToString("N"), accessExpiration),
            RefreshToken = Build("refresh", refreshTokenId, refreshExpiration),
            ExpiryInMinutes = (int)Math.Ceiling((accessExpiration - clock.GetUtcNow().UtcDateTime).TotalMinutes) };
    }
}
