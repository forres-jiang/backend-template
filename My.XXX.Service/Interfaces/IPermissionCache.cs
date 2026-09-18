using System;
using System.Collections.Generic;

namespace My.XXX.Service.Interfaces;

public interface IPermissionCache
{
    bool TryGet(string userId, out List<string> paths);
    void Set(string userId, List<string> paths, TimeSpan expiration);
    void Remove(string userId);
}
