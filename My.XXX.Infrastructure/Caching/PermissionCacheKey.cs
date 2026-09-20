using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace My.XXX.Infrastructure.Caching;

public static class PermissionCacheKey
{
    public static string Create(string prefix, string userId, IEnumerable<Guid> roleIds, long revision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var roles = string.Join(",", roleIds.Distinct().OrderBy(id => id).Select(id => id.ToString("N")));
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId + "\n" + roles)));
        return prefix + ":" + revision.ToString(CultureInfo.InvariantCulture) + ":" + identity;
    }
}

