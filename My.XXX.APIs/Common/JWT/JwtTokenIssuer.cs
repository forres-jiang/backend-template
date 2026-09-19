using Microsoft.Extensions.Options;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using System;

namespace My.XXX.APIs.Common.JWT;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtConfig _config;
    public JwtTokenIssuer(IOptionsMonitor<JwtConfig> config) => _config = config.CurrentValue;
    public TokenPair Issue(UserInfo user) => Map(JwtTokenBuilder.CreateTokens(_config, user));
    public TokenPair Refresh(UserInfo user, DateTime refreshExpiration) =>
        Map(JwtTokenBuilder.RefreshTokens(_config, user, refreshExpiration));
    private TokenPair Map(Tokens tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        RefreshToken = tokens.RefreshToken,
        ExpiryInMinutes = _config.ExpiryInMinutes
    };
}
