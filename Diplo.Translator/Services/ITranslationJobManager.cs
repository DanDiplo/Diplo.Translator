using Diplo.Translator.Models;

namespace Diplo.Translator.Services;

public interface ITranslationJobManager
{
    bool TryStart(TranslationJobRequest request, Guid userKey, out TranslationJob job);

    TranslationJob? Get(Guid jobId);

    bool Cancel(Guid jobId);
}
