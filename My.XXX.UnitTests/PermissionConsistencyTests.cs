using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Infrastructure.Caching;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Ports;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.UnitTests;

[TestClass]
public class PermissionConsistencyTests
{
    [TestMethod]
    public async Task CommittedRevisionAndRoleChangesCannotReuseOldGrants()
    {
        long revision = 1;
        var reads = 0;
        var allowed = new List<string> { "Menu/Edit" };
        var repository = Repository((method, _) => method.Name switch
        {
            nameof(IPermissionStore.GetPermissionRevisionAsync) => Task.FromResult(revision),
            nameof(IPermissionStore.GetPermissionCodesAsync) => Read(),
            _ => throw new NotSupportedException(method.Name)
        });
        Task<List<string>> Read() { reads++; return Task.FromResult(new List<string>(allowed)); }
        var cache = new MemoryCache();
        var query = Query(repository, cache);
        var roles = new List<Guid> { Guid.NewGuid() };
        CollectionAssert.AreEqual(allowed, await query.GetPermissionCodesAsync(roles, "user"));
        await query.GetPermissionCodesAsync(roles, "user");
        Assert.AreEqual(1, reads, "A stable revision should use the cached projection.");
        revision++;
        allowed.Clear();
        Assert.HasCount(0, await query.GetPermissionCodesAsync(roles, "user"));
        Assert.AreEqual(2, reads);
        roles.Add(Guid.NewGuid());
        await query.GetPermissionCodesAsync(roles, "user");
        Assert.AreEqual(3, reads, "Role claims are part of the cache identity.");
    }

    [TestMethod]
    public async Task RevisionChangingDuringReadDoesNotPublishOldPermissions()
    {
        long revision = 1;
        var reads = 0;
        var repository = Repository((method, _) =>
        {
            if (method.Name == nameof(IPermissionStore.GetPermissionRevisionAsync)) return Task.FromResult(revision);
            reads++;
            if (reads == 1) { revision++; return Task.FromResult(new List<string> { "old/grant" }); }
            return Task.FromResult(new List<string>());
        });
        var cache = new MemoryCache();
        var result = await Query(repository, cache).GetPermissionCodesAsync(new() { Guid.NewGuid() }, "user");
        Assert.HasCount(0, result);
        Assert.AreEqual(2, reads);
        Assert.HasCount(1, cache.Values);
        foreach (var value in cache.Values.Values) Assert.HasCount(0, value);
    }

    [TestMethod]
    public async Task RevisionChangingDuringCacheHitRejectsStaleEntry()
    {
        long revision = 1;
        var roles = new List<Guid> { Guid.NewGuid() };
        var cache = new MemoryCache();
        cache.Values[PermissionCacheKey.Create("test", "user", roles, 1)] = new() { "old/grant" };
        cache.OnGet = () => revision = 2;
        var repository = Repository((method, _) => method.Name == nameof(IPermissionStore.GetPermissionRevisionAsync)
            ? Task.FromResult(revision) : Task.FromResult(new List<string>()));
        Assert.HasCount(0, await Query(repository, cache).GetPermissionCodesAsync(roles, "user"));
    }

    [TestMethod]
    public async Task DatabaseModeDoesNotNeedRevisionTableOrRedis()
    {
        var repository = Repository((method, args) =>
        {
            Assert.AreEqual(nameof(IPermissionStore.GetPermissionCodesAsync), method.Name);
            return Task.FromResult(new List<string> { "current/grant" });
        });
        var cache = new MemoryCache { OnGet = () => throw new AssertFailedException("Redis must not be read.") };
        var query = Query(repository, cache, PermissionDataCache.None);
        CollectionAssert.AreEqual(new[] { "current/grant" }, await query.GetPermissionCodesAsync(new(), "user"));
    }

    [TestMethod]
    public async Task DatabaseFailureCannotBeHiddenByCachedGrants()
    {
        var repository = Repository((_, _) => Task.FromException<long>(new InvalidOperationException("offline")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Query(repository, new()).GetPermissionCodesAsync(new(), "user"));
    }

    [TestMethod]
    public async Task LogoutDeletesVersionedAndLegacyKeysAndPropagatesFailures()
    {
        var roles = new List<Guid> { Guid.NewGuid() };
        var cache = new MemoryCache();
        var key = PermissionCacheKey.Create("test", "user", roles, 7);
        cache.Values[key] = new();
        cache.Values["user"] = new();
        var repository = Repository((_, _) => Task.FromResult(7L));
        var query = Query(repository, cache);
        await query.RemoveCachedPermissionsAsync(roles, "user");
        Assert.HasCount(0, cache.Values);
        cache.FailDelete = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => query.RemoveCachedPermissionsAsync(roles, "user"));
    }

    [TestMethod]
    public void CacheIdentityCanonicalizesRolesAndSeparatesUsersAndRevisions()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var key = PermissionCacheKey.Create("test", "user", new[] { first, second }, 1);
        Assert.AreEqual(key, PermissionCacheKey.Create("test", "user", new[] { second, first, first }, 1));
        Assert.AreNotEqual(key, PermissionCacheKey.Create("test", "user", new[] { first, second }, 2));
        Assert.AreNotEqual(key, PermissionCacheKey.Create("test", "another", new[] { first, second }, 1));
    }

    private static IPermissionQuery Query(IPermissionStore repository, MemoryCache cache, PermissionDataCache mode = PermissionDataCache.Redis) =>
        mode == PermissionDataCache.None ? new PermissionQuery(repository) : new CachedPermissionQuery(repository, cache,
            Options.Create(new PermissionCacheOptions { KeyPrefix = "test", ExpiryInMinutes = 2 }));
    private static IPermissionStore Repository(Func<MethodInfo, object[], object> handler)
    {
        var repository = DispatchProxy.Create<IPermissionStore, RepositoryProxy>();
        ((RepositoryProxy)repository).Handler = handler;
        return repository;
    }
    public class RepositoryProxy : DispatchProxy
    {
        public Func<MethodInfo, object[], object> Handler { get; set; }
        protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
    }
    private sealed class Monitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string name) => value;
        public IDisposable OnChange(Action<T, string> listener) => null;
    }
    private sealed class MemoryCache : IPermissionCache
    {
        public readonly Dictionary<string, List<string>> Values = new();
        public Action OnGet;
        public bool FailDelete;
        public Task<List<string>> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            OnGet?.Invoke();
            return Task.FromResult(Values.TryGetValue(key, out var value) ? value : null);
        }
        public Task SetAsync(string key, List<string> paths, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            Assert.AreEqual(TimeSpan.FromMinutes(2), expiry, "Permission TTL must not depend on JWT expiry.");
            Values[key] = new(paths); return Task.CompletedTask;
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (FailDelete) throw new InvalidOperationException("Redis unavailable.");
            Values.Remove(key); return Task.CompletedTask;
        }
    }
}
