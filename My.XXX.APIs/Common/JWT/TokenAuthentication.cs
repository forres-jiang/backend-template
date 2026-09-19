using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using My.XXX.Shared;
using System;
using System.Text;
using System.Threading.Tasks;

namespace My.XXX.APIs.Common.JWT;

public static class TokenAuthentication
{
    public static void Configure(JwtBearerOptions options, JwtConfig config, string purpose)
    {
        if (config == null || Encoding.UTF8.GetByteCount(config.Secret ?? "") < 32 ||
            string.IsNullOrWhiteSpace(config.Issuer) || string.IsNullOrWhiteSpace(config.Audience))
            throw new InvalidOperationException("Configure JwtConfig Secret (at least 32 UTF-8 bytes), Issuer and Audience through a secret provider.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Secret)),
            ValidIssuer = config.Issuer,
            ValidAudience = purpose == "refresh" ? config.Audience + ":refresh" : config.Audience,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.FindFirst("token_use")?.Value != purpose)
                    context.Fail("Invalid token purpose.");
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { state = "0", message = "Authentication required." });
            }
        };
    }
}
