namespace Diplo.Translator.Models;

public sealed record TranslatorLanguageDto(
    int Id,
    string IsoCode,
    string CultureName,
    string DisplayName,
    bool IsDefault);

public sealed record TranslatorSettingsDto(
    bool HasConfiguredChatProfileAlias,
    string? ChatProfileAlias);

public sealed record TranslationJobCreatedDto(Guid JobId);

public sealed record TranslationJobDto(
    Guid Id,
    string FromCulture,
    bool Overwrite,
    string Status,
    string StatusMessage,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc,
    TranslationJobProgress Progress,
    IReadOnlyCollection<string> Messages,
    IReadOnlyCollection<string> Errors);
