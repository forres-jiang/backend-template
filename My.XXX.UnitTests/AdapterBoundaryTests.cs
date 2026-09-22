using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Infrastructure;
using My.XXX.Infrastructure.Caching;
using My.XXX.Infrastructure.Logging;
using My.XXX.Services;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Operations.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class AdapterBoundaryTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void PermissionAdapterSelectionSurvivesEitherRegistrationOrder(bool adaptersFirst)
    {
        var services = new ServiceCollection();
        if (!adaptersFirst) services.AddBusinessServices();
        services.AddPermissionCaching("localhost:6379", enabled: true);
        if (adaptersFirst) services.AddBusinessServices();
        Assert.AreEqual(typeof(CachedPermissionQuery), services.Last(d => d.ServiceType == typeof(IPermissionQuery)).ImplementationType);
        services.AddPermissionCaching(null, enabled: false);
        Assert.AreEqual(typeof(PermissionQuery), services.Last(d => d.ServiceType == typeof(IPermissionQuery)).ImplementationType);
    }

    [TestMethod]
    public async Task DatabaseLogFailureStillWritesTextAndDoesNotEscape()
    {
        var logger = new TestLogger<RequestLogWriter>();
        var options = new Monitor<RequestLogOptions>(new() { RequestLogStorageType = StorageTypeEnum.TextAndSQL });
        var writer = new RequestLogWriter(new FailingRepository(), options, logger);
        await writer.WriteAsync(new MetricsInfo { RequestId = Guid.NewGuid(), TotalTime = 7 });
        Assert.IsTrue(logger.Messages.Any(m => m.Contains("Failed to persist")));
        Assert.IsTrue(logger.Messages.Any(m => m.Contains("completed")));
        Assert.IsFalse(logger.Messages.Any(m => m.Contains("sensitive connection")));
    }

    private sealed class FailingRepository : IOperationRepository
    {
        public Task Save(MetricsInfo record) => throw new InvalidOperationException("sensitive connection");
        public Task<Paged<OperationDto>> Search(My.XXX.Services.Operations.Models.OperationSearch query) => throw new NotSupportedException();
    }
    private sealed class Monitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string name) => value;
        public IDisposable OnChange(Action<T, string> listener) => null;
    }
    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception exception, Func<TState, Exception, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
