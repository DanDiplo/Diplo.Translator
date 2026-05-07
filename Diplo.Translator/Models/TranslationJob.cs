namespace Diplo.Translator.Models;

public sealed class TranslationJob : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly object syncRoot = new();
    private readonly Queue<string> messages = new();
    private readonly Queue<string> errors = new();

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

    public DateTimeOffset? CompletedUtc { get; set; }

    public TranslationJobStatus Status { get; private set; } = TranslationJobStatus.Pending;

    public string StatusMessage { get; private set; } = "Waiting to start...";

    public TranslationJobProgress Progress { get; } = new();

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
        Status = status;
        if (!string.IsNullOrWhiteSpace(message))
        {
            StatusMessage = message;
        }
    }

    public void SetCurrentItem(string? itemKey)
        => Progress.CurrentItem = itemKey;

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
        if (Status is TranslationJobStatus.Completed or TranslationJobStatus.CompletedWithErrors or TranslationJobStatus.Failed or TranslationJobStatus.Cancelled)
        {
            return;
        }

        SetStatus(TranslationJobStatus.Cancelled, "Translation job cancelled.");
        cancellationTokenSource.Cancel();
    }

    public void Dispose()
        => cancellationTokenSource.Dispose();
}
