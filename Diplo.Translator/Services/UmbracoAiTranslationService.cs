using Diplo.Translator.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;

namespace Diplo.Translator.Services;

public sealed class UmbracoAiTranslationService : ITranslationService
{
    private readonly IAIChatService chatService;
    private readonly TranslatorConfig config;
    private readonly ILogger<UmbracoAiTranslationService> logger;

    public UmbracoAiTranslationService(
        IAIChatService chatService,
        IOptions<TranslatorConfig> config,
        ILogger<UmbracoAiTranslationService> logger)
    {
        this.chatService = chatService;
        this.config = config.Value;
        this.logger = logger;
    }

    public async Task<TranslationResponse> TranslateAsync(string text, string to, string? from = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are a strict translation engine. Return only the translated text. Do not add markdown, commentary, labels, surrounding quotes, or explanations. Preserve placeholders, dictionary tokens, URLs, HTML tags, whitespace-sensitive fragments, and formatting markers."),
                new(ChatRole.User, BuildPrompt(text, to, from))
            };

            var response = await chatService.GetChatResponseAsync(
                builder =>
                {
                    builder.WithAlias("diplo-translator");
                    builder.WithName("Diplo Translator");

                    if (!string.IsNullOrWhiteSpace(config.ChatProfileAlias))
                    {
                        builder.WithProfile(config.ChatProfileAlias);
                    }
                },
                messages,
                cancellationToken);

            var translatedText = response.Text?.Trim();
            return new TranslationResponse
            {
                IsSuccess = !string.IsNullOrWhiteSpace(translatedText),
                Text = translatedText,
                From = from,
                To = to,
                Message = string.IsNullOrWhiteSpace(translatedText) ? "The AI provider returned an empty translation." : null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI translation failed from {FromCulture} to {ToCulture}.", from, to);
            return new TranslationResponse
            {
                IsSuccess = false,
                From = from,
                To = to,
                Message = ex.Message
            };
        }
    }

    private static string BuildPrompt(string text, string to, string? from)
        => $"""
           Source culture: {from ?? "auto-detect"}
           Target culture: {to}

           Translate this text:
           {text}
           """;
}
