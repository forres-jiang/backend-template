using System;
using System.Collections.Generic;
using System.Linq;

namespace My.XXX.Services.Authentication.Models;

/// <summary>An immutable identity snapshot, independent of navigation and HTTP responses.</summary>
public sealed record UserIdentity
{
    private readonly IReadOnlyList<string> roles = Array.Empty<string>();
    private readonly IReadOnlyList<Guid> roleIds = Array.Empty<Guid>();
    public string UserId { get; init; }
    public string UserName { get; init; }
    public string Email { get; init; }
    public IReadOnlyList<string> Roles { get => roles; init => roles = Array.AsReadOnly(value?.ToArray() ?? Array.Empty<string>()); }
    public IReadOnlyList<Guid> RoleIds { get => roleIds; init => roleIds = Array.AsReadOnly(value?.ToArray() ?? Array.Empty<Guid>()); }
}
