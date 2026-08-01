using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lanflix.Modules.Administration;

public sealed class AdminJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(32)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false
    });
    public bool TryEnqueue(Guid id) => _channel.Writer.TryWrite(id);
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class AdminJobWorker(
    AdminJobQueue queue,
    IServiceScopeFactory scopes,
    ILogger<AdminJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var id in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<IAdministrationDbContext>();
                    var operations = scope.ServiceProvider.GetRequiredService<IAdministrationOperations>();
                    var job = await db.BackgroundJobRuns.SingleOrDefaultAsync(item => item.Id == id, stoppingToken);
                    if (job is null) continue;
                    job.Start();
                    await db.SaveChangesAsync(stoppingToken);
                    try
                    {
                        job.Complete(await operations.ExecuteJobAsync(job.Name, stoppingToken));
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        logger.LogError(exception, "Administration job {JobName} failed", job.Name);
                        job.Fail(exception.Message);
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
