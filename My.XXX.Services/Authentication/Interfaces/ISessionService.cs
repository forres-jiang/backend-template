using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Authentication.Models;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Services.Authentication.Interfaces;

public interface ISessionService
{
    Task<Result<ActiveSession>> ValidateAsync(string sessionId, string userId, string refreshTokenId = null, CancellationToken cancellationToken = default);
    Task<TokenPair> IssueAsync(string userId, CancellationToken cancellationToken = default);
}
