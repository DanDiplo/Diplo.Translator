namespace Diplo.Translator.Models;

public enum TranslationJobStatus
{
    Pending,
    Running,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled
}
