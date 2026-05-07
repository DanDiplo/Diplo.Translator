namespace Diplo.Translator.Models;

public sealed class TranslationJob : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly object syncRoot = new();
    private readonly Queue<string> messages = new();
    private readonly Queue<string> errors = new();
    private readonly TranslationJobProgress progress = new();
    private DateTimeOffset? completedUtc;
    private TranslationJobStatus status = TranslationJobStatus.Pending;
    private string statusMessage = "Waiting to start...";

    public TranslationJob(string fromCulture, bool overwrite, Guid userKey)
    {
        Id = Guid.NewGuid();
        FromCulture = fromCulture;
        Overwrite = overwrite;
        UserKey = userKey;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string FromCulture { get; }

    public bool Overwrite { get; }

    public Guid UserKey { get; }

    public DateTimeOffset CreatedUtc { get; }

    public DateTimeOffset? CompletedUtc
    {
        get
        {
            lock (syncRoot)
            {
                return completedUtc;
            }
        }
    }

    public TranslationJobStatus Status
    {
        get
        {
            lock (syncRoot)
            {
                return status;
            }
        }
    }

    public string StatusMessage
    {
        get
        {
            lock (syncRoot)
            {
                return statusMessage;
            }
        }
    }

    public TranslationJobProgress Progress
    {
        get
        {
            lock (syncRoot)
            {
                return progress.Clone();
            }
        }
    }

    public CancellationToken CancellationToken => cancellationTokenSource.Token;

    public IReadOnlyCollection<string> Messages
    {
        get
        {
            lock (syncRoot)
            {
                return messages.ToArray();
            }
        }
    }

    public IReadOnlyCollection<string> Errors
    {
        get
        {
            lock (syncRoot)
            {
                return errors.ToArray();
            }
        }
    }

    public void SetStatus(TranslationJobStatus status, string? message = null)
    {
        lock (syncRoot)
        {
            this.status = status;
            if (!string.IsNullOrWhiteSpace(message))
            {
                statusMessage = message;
            }
        }
    }

    public void SetCurrentItem(string? itemKey)
    {
        lock (syncRoot)
        {
            progress.CurrentItem = itemKey;
        }
    }

    public void SetDictionaryCount(int count)
    {
        lock (syncRoot)
        {
            progress.DictionaryCount = count;
        }
    }

    public void IncrementProcessedItemCount()
    {
        lock (syncRoot)
        {
            progress.ProcessedItemCount++;
        }
    }

    public void IncrementAttemptedTranslationCount()
    {
        lock (syncRoot)
        {
            progress.AttemptedTranslationCount++;
        }
    }

    public void IncrementTranslationCount()
    {
        lock (syncRoot)
        {
            progress.TranslationCount++;
        }
    }

    public void IncrementSkippedCount(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        lock (syncRoot)
        {
            progress.SkippedCount += count;
        }
    }

    public void IncrementErrorCount()
    {
        lock (syncRoot)
        {
            progress.ErrorCount++;
        }
    }

    public int ErrorCount
    {
        get
        {
            lock (syncRoot)
            {
                return progress.ErrorCount;
            }
        }
    }

    public void MarkCompleted()
    {
        lock (syncRoot)
        {
            completedUtc ??= DateTimeOffset.UtcNow;
        }
    }

    public void AddMessage(string message)
    {
        lock (syncRoot)
        {
            messages.Enqueue(message);
            while (messages.Count > 20)
            {
                messages.Dequeue();
            }
        }
    }

    public void AddError(string message)
    {
        lock (syncRoot)
        {
            errors.Enqueue(message);
            while (errors.Count > 20)
            {
                errors.Dequeue();
            }
        }
    }

    public void Cancel()
    {
        lock (syncRoot)
        {
            if (status is TranslationJobStatus.Completed or TranslationJobStatus.CompletedWithErrors or TranslationJobStatus.Failed or TranslationJobStatus.Cancelled)
            {
                return;
            }

            status = TranslationJobStatus.Cancelled;
            statusMessage = "Translation job cancelled.";
        }

        cancellationTokenSource.Cancel();
    }

    public void Dispose()
        => cancellationTokenSource.Dispose();
}
