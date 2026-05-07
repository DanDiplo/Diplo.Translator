using System.Collections.Concurrent;
using Diplo.Translator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diplo.Translator.Services;

public sealed class TranslationJobManager : ITranslationJobManager
{
    private static readonly TimeSpan CompletedJobRetention = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<Guid, TranslationJob> jobs = new();
    private readonly object startLock = new();
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<TranslationJobManager> logger;

    public TranslationJobManager(IServiceScopeFactory serviceScopeFactory, ILogger<TranslationJobManager> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public bool TryStart(TranslationJobRequest request, Guid userKey, out TranslationJob job)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (startLock)
        {
            PruneCompletedJobs();

            if (jobs.Values.Any(IsActive))
            {
                job = null!;
                return false;
            }

            job = new TranslationJob(request.FromCulture, request.Overwrite, userKey);
            jobs[job.Id] = job;
        }

        var startedJob = job;
        _ = Task.Run(() => RunAsync(startedJob));
        return true;
    }

    public TranslationJob? Get(Guid jobId)
    {
        lock (startLock)
        {
            PruneCompletedJobs();
            return jobs.TryGetValue(jobId, out var job) ? job : null;
        }
    }

    public bool Cancel(Guid jobId)
    {
        lock (startLock)
        {
            PruneCompletedJobs();

            if (!jobs.TryGetValue(jobId, out var job))
            {
                return false;
            }

            job.Cancel();
            return true;
        }
    }

    private async Task RunAsync(TranslationJob job)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IDictionaryTranslationService>();
            await service.TranslateDictionaryAsync(job, job.CancellationToken);

            if (job.Status != TranslationJobStatus.Cancelled)
            {
                var status = job.ErrorCount > 0 ? TranslationJobStatus.CompletedWithErrors : TranslationJobStatus.Completed;
                job.SetStatus(status, job.Progress.Message);
            }
        }
        catch (OperationCanceledException)
        {
            job.SetStatus(TranslationJobStatus.Cancelled, "Translation job cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Translation job {JobId} failed.", job.Id);
            job.AddError(ex.Message);
            job.SetStatus(TranslationJobStatus.Failed, ex.Message);
        }
        finally
        {
            job.MarkCompleted();
            _ = RemoveAfterRetentionAsync(job.Id, job.CompletedUtc!.Value);
        }
    }

    private void PruneCompletedJobs()
    {
        var removeBefore = DateTimeOffset.UtcNow.Subtract(CompletedJobRetention);

        foreach (var job in jobs.Values.Where(job => job.CompletedUtc is not null && job.CompletedUtc < removeBefore))
        {
            if (jobs.TryRemove(job.Id, out var removedJob))
            {
                removedJob.Dispose();
            }
        }
    }

    private static bool IsActive(TranslationJob job)
        => job.CompletedUtc is null;

    private async Task RemoveAfterRetentionAsync(Guid jobId, DateTimeOffset completedUtc)
    {
        try
        {
            await Task.Delay(CompletedJobRetention);

            if (!jobs.TryGetValue(jobId, out var job) || job.CompletedUtc != completedUtc)
            {
                return;
            }

            lock (startLock)
            {
                if (jobs.TryGetValue(jobId, out job) && job.CompletedUtc == completedUtc && jobs.TryRemove(jobId, out var removedJob))
                {
                    removedJob.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not prune completed translation job {JobId}.", jobId);
        }
    }
}
