using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Operations.Interfaces;
using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace My.XXX.Infrastructure.Logging;

/// <summary>Bounded, best-effort telemetry. Full queues reject new records; this is not an audit log.</summary>
public sealed class QueuedRequestLogWriter : IRequestLogWriter, IHostedService, IDisposable
{
    private static readonly Meter Meter = new("My.XXX.RequestLogging");
    private static readonly Counter<long> Dropped = Meter.CreateCounter<long>("request_logs.dropped");
    private readonly Channel<MetricsInfo> queue;
    private readonly IServiceScopeFactory scopes;
    private readonly ILogger<QueuedRequestLogWriter> logger;
    private readonly TimeSpan writeTimeout;
    private readonly CancellationTokenSource stopping = new();
    private Task worker = Task.CompletedTask;
    private int disposed;

    public QueuedRequestLogWriter(IServiceScopeFactory scopes, IOptions<RequestLogOptions> options,
        ILogger<QueuedRequestLogWriter> logger)
    {
        var config = options.Value;
        if (config.QueueCapacity <= 0 || config.WriteTimeoutSeconds <= 0)
            throw new InvalidOperationException("Request log queue capacity and write timeout must be positive.");
        queue = Channel.CreateBounded<MetricsInfo>(new BoundedChannelOptions(config.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });
        this.scopes = scopes;
        this.logger = logger;
        writeTimeout = TimeSpan.FromSeconds(config.WriteTimeoutSeconds);
    }

    public Task WriteAsync(MetricsInfo record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!queue.Writer.TryWrite(record)) Dropped.Add(1);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        worker = Task.Run(Consume);
        return Task.CompletedTask;
    }

    private async Task Consume()
    {
        try
        {
            await foreach (var record in queue.Reader.ReadAllAsync(stopping.Token))
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    timeout.CancelAfter(writeTimeout);
                    await scope.ServiceProvider.GetRequiredService<RequestLogWriter>().WriteAsync(record, timeout.Token);
                }
                catch (Exception exception)
                {
                    logger.LogWarning("Request log sink failed: {ExceptionType}", exception.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Writer.TryComplete();
        try { await worker.WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopping.Cancel();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        queue.Writer.TryComplete();
        stopping.Cancel();
        stopping.Dispose();
    }
}
