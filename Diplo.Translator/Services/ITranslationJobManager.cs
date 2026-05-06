using Diplo.Translator.Models;

namespace Diplo.Translator.Services;

public interface ITranslationJobManager
{
    TranslationJob Start(TranslationJobRequest request);

    TranslationJob? Get(Guid jobId);

    bool Cancel(Guid jobId);
}
