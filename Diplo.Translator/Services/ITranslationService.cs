using Diplo.Translator.Models;

namespace Diplo.Translator.Services;

public interface ITranslationService
{
    Task<TranslationResponse> TranslateAsync(string text, string to, string? from = null, CancellationToken cancellationToken = default);
}
