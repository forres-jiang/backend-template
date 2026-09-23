using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Common;
using My.XXX.APIs.Models;
using My.XXX.Contracts.DTOs;
using My.XXX.Infrastructure;
using My.XXX.Infrastructure.Logging;
using My.XXX.Persistences.Mapping;
using My.XXX.Services.Operations.Ports;
using My.XXX.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.UnitTests;

[TestClass]
public class ArchitectureImprovementsTests
{
    [TestMethod]
    public void IdentityStorageReadsLegacyAndVersionedRowsWithoutPersistingMenus()
    {
        var legacy = IdentitySnapshot.Read("{\"UserId\":\"u\",\"Roles\":null,\"RoleIds\":null,\"Menus\":[{\"Id\":1}]}");
        Assert.HasCount(0, legacy.Roles);
        Assert.IsNull(legacy.Menus);
        var json = IdentitySnapshot.Write(new UserInfo { UserId = "u", Menus = new() { new MenuDto() } });
        Assert.AreEqual(1, (int)JObject.Parse(json)["Version"]);
        Assert.IsNull(JObject.Parse(json)["Menus"]);
        Assert.AreEqual("u", IdentitySnapshot.Read(json).UserId);
        Assert.ThrowsExactly<InvalidOperationException>(() => IdentitySnapshot.Read("{\"Version\":2,\"UserId\":\"u\"}"));
    }

    [TestMethod]
    public void ExplicitFailurePreservesErrorCodeStatusAndTraceWithoutLeakingMetadata()
    {
        var result = Result.Fail<string>(new BusinessError("Missing", code: "Menu.NotFound").WithMetadata("secret", "hidden"));
        var http = (ObjectResult)result.ToHttpResult("trace").Result;
        Assert.AreEqual(404, http.StatusCode);
        var response = (ApiResponse<string>)http.Value;
        Assert.AreEqual("Menu.NotFound", response.Code);
        Assert.AreEqual("trace", response.TraceId);
        Assert.IsNull(response.Data);
        Assert.IsFalse(Newtonsoft.Json.JsonConvert.SerializeObject(response).Contains("hidden"));
        Assert.IsNull(JObject.FromObject(result.ToApiResult())["Code"], "Legacy envelope must remain unchanged.");
    }

    [TestMethod]
    public async Task QueueRejectsOverflowAndDrainsUsingIndependentDisposedScopes()
    {
        long dropped = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Name == "request_logs.dropped") meter.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref dropped, value));
        listener.Start();
        var state = new SinkState();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternalAdapters();
        services.Configure<RequestLogOptions>(o => { o.QueueCapacity = 1; o.RequestLogStorageType = StorageTypeEnum.SQL; });
        services.AddSingleton(state);
        services.AddScoped<IOperationRepository, RecordingSink>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var queue = provider.GetRequiredService<QueuedRequestLogWriter>();
        var first = new MetricsInfo { RequestId = Guid.NewGuid() };
        await queue.WriteAsync(first);
        await queue.WriteAsync(new MetricsInfo());
        Assert.AreEqual(1L, dropped);
        Assert.AreEqual(0, state.Saved.Count);
        await queue.StartAsync(CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await queue.StopAsync(timeout.Token);
        Assert.HasCount(1, state.Saved);
        Assert.AreEqual(first.RequestId, state.Saved[0]);
        Assert.AreEqual(1, state.Disposed);
    }

    private sealed class SinkState
    {
        public List<Guid?> Saved { get; } = new();
        public int Disposed;
    }
    private sealed class RecordingSink(SinkState state) : IOperationRepository, IDisposable
    {
        public Task Save(MetricsInfo record, CancellationToken cancellationToken = default)
        {
            state.Saved.Add(record.RequestId);
            return Task.CompletedTask;
        }
        public Task<Paged<OperationDto>> Search(My.XXX.Services.Operations.Models.OperationSearch query) => throw new NotSupportedException();
        public void Dispose() => state.Disposed++;
    }
}
