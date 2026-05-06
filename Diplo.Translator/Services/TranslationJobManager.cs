using System.Collections.Concurrent;
using Diplo.Translator.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diplo.Translator.Services;

public sealed class TranslationJobManager : ITranslationJobManager
{
    private readonly ConcurrentDictionary<Guid, TranslationJob> jobs = new();
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<TranslationJobManager> logger;

    public TranslationJobManager(IServiceScopeFactory serviceScopeFactory, ILogger<TranslationJobManager> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public TranslationJob Start(TranslationJobRequest request)
    {
        var job = new TranslationJob(request.FromCulture, request.Overwrite);
        jobs[job.Id] = job;

        _ = Task.Run(() => RunAsync(job));
        return job;
    }

    public TranslationJob? Get(Guid jobId)
        => jobs.TryGetValue(jobId, out var job) ? job : null;

    public bool Cancel(Guid jobId)
    {
        if (!jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        job.Cancel();
        return true;
    }

    private async Task RunAsync(TranslationJob job)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IDictionaryTranslationService>();
            await service.TranslateDictionaryAsync(job, job.CancellationToken);

            if (job.Status != TranslationJobStatus.Cancelled)
            {
                var status = job.Progress.ErrorCount > 0 ? TranslationJobStatus.CompletedWithErrors : TranslationJobStatus.Completed;
                job.SetStatus(status, job.Progress.Message);
                job.CompletedUtc = DateTimeOffset.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            job.SetStatus(TranslationJobStatus.Cancelled, "Translation job cancelled.");
            job.CompletedUtc = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Translation job {JobId} failed.", job.Id);
            job.AddError(ex.Message);
            job.SetStatus(TranslationJobStatus.Failed, ex.Message);
            job.CompletedUtc = DateTimeOffset.UtcNow;
        }
    }
}
