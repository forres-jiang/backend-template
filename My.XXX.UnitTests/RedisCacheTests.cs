using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Infrastructure;
using My.XXX.Service.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class RedisCacheTests
{
    [TestMethod]
    public async Task ReadsExistingJsonAndDistinguishesMissingFromEmptyPermissions()
    {
        RedisValue value = "[\"Menu/Get\",\"用户/查询\"]";
        var cache = CreateCache((method, args) =>
        {
            Assert.AreEqual("StringGetAsync", method.Name);
            Assert.AreEqual("user-1", ((RedisKey)args[0]).ToString());
            return Task.FromResult(value);
        });
        CollectionAssert.AreEqual(new[] { "Menu/Get", "用户/查询" }, await cache.GetAsync("user-1"));
        value = "[]";
        Assert.HasCount(0, await cache.GetAsync("user-1"));
        value = RedisValue.Null;
        Assert.IsNull(await cache.GetAsync("user-1"));
    }

    [TestMethod]
    public async Task WritesJsonWithAtomicExpiryAndDeletesTheSameKey()
    {
        var calls = new List<string>();
        var ttl = TimeSpan.FromMinutes(20);
        var cache = CreateCache((method, args) =>
        {
            calls.Add(method.Name);
            Assert.AreEqual("user-1", ((RedisKey)args[0]).ToString());
            if (method.Name == "StringSetAsync")
            {
                Assert.AreEqual("[\"Menu/Get\"]", ((RedisValue)args[1]).ToString());
                Assert.AreEqual((Expiration)ttl, (Expiration)args[2]);
            }
            else Assert.AreEqual("KeyDeleteAsync", method.Name);
            return Task.FromResult(true);
        });
        await cache.SetAsync("user-1", new List<string> { "Menu/Get" }, ttl);
        await cache.RemoveAsync("user-1");
        CollectionAssert.AreEqual(new[] { "StringSetAsync", "KeyDeleteAsync" }, calls);
    }

    [TestMethod]
    public async Task RejectsNonPositiveExpiryAndCancelledCommandsBeforeSending()
    {
        var cache = CreateCache((_, _) => throw new AssertFailedException("No command should be sent."));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cache.SetAsync("user-1", new(), TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cache.SetAsync("user-1", new(), TimeSpan.FromSeconds(-1)));
        var cancelled = new CancellationToken(true);
        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.GetAsync("user-1", cancelled));
        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.SetAsync("user-1", new(), TimeSpan.FromMinutes(1), cancelled));
        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.RemoveAsync("user-1", cancelled));
    }

    [TestMethod]
    public async Task DeleteFailureIsNotReportedAsSuccessfulLogout()
    {
        var cache = CreateCache((_, _) => Task.FromException<bool>(new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Unavailable")));
        await Assert.ThrowsAsync<RedisConnectionException>(() => cache.RemoveAsync("user-1"));
    }

    [TestMethod]
    public async Task DatabaseModeResolvesWithoutRedisAndRedisModeRequiresConfiguration()
    {
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings:Default"] = "Server=localhost;Database=test;Integrated Security=true"
        };
        var services = new ServiceCollection();
        services.AddDBs(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var provider = services.BuildServiceProvider();
        Assert.IsNull(provider.GetService<IConnectionMultiplexer>());
        var cache = provider.GetRequiredService<IPermissionCache>();
        Assert.IsInstanceOfType<NullPermissionCache>(cache);
        Assert.IsNull(await cache.GetAsync("user-1"));

        settings["AppConfig:PermissionDataCache"] = "1";
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddDBs(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
    }

    private static PermissionCache CreateCache(Func<MethodInfo, object[], object> invoke)
    {
        var database = DispatchProxy.Create<IDatabase, RedisProxy>();
        ((RedisProxy)database).Handler = invoke;
        var connection = DispatchProxy.Create<IConnectionMultiplexer, RedisProxy>();
        ((RedisProxy)connection).Handler = (method, _) =>
            method.Name == "GetDatabase" ? database : throw new NotSupportedException(method.Name);
        return new PermissionCache(connection);
    }

    public class RedisProxy : DispatchProxy
    {
        public Func<MethodInfo, object[], object> Handler { get; set; }
        protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args);
    }
}
