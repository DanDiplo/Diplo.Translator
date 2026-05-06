using Asp.Versioning;
using Diplo.Translator.Models;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace Diplo.Translator.Controllers;

[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("diplo-translator")]
[ApiExplorerSettings(GroupName = "DiploTranslator")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public sealed class TranslationApiController : ManagementApiControllerBase
{
    private readonly IDictionaryTranslationService dictionaryTranslationService;
    private readonly ITranslationJobManager jobManager;
    private readonly TranslatorConfig config;

    public TranslationApiController(
        IDictionaryTranslationService dictionaryTranslationService,
        ITranslationJobManager jobManager,
        IOptions<TranslatorConfig> config)
    {
        this.dictionaryTranslationService = dictionaryTranslationService;
        this.jobManager = jobManager;
        this.config = config.Value;
    }

    [HttpGet("languages")]
    [ProducesResponseType<IEnumerable<TranslatorLanguageDto>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TranslatorLanguageDto>> GetLanguages()
        => Ok(dictionaryTranslationService.GetAllLanguages()
            .Select(language => new TranslatorLanguageDto(
                language.Id,
                language.IsoCode,
                language.CultureInfo?.Name ?? language.IsoCode,
                language.CultureInfo?.DisplayName ?? language.IsoCode,
                language.IsDefault)));

    [HttpGet("settings")]
    [ProducesResponseType<TranslatorSettingsDto>(StatusCodes.Status200OK)]
    public ActionResult<TranslatorSettingsDto> GetSettings()
        => Ok(new TranslatorSettingsDto(
            !string.IsNullOrWhiteSpace(config.ChatProfileAlias),
            config.ChatProfileAlias));

    [HttpPost("jobs")]
    [ProducesResponseType<TranslationJobCreatedDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TranslationJobCreatedDto> StartJob([FromBody] TranslationJobRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FromCulture))
        {
            return BadRequest("A source culture is required.");
        }

        var language = dictionaryTranslationService.GetAllLanguages()
            .FirstOrDefault(x => x.IsoCode.InvariantEquals(request.FromCulture));

        if (language is null)
        {
            return BadRequest($"Language '{request.FromCulture}' was not found.");
        }

        var job = jobManager.Start(request);
        return Accepted(new TranslationJobCreatedDto(job.Id));
    }

    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType<TranslationJobDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TranslationJobDto> GetJob(Guid jobId)
    {
        var job = jobManager.Get(jobId);
        return job is null ? NotFound() : Ok(ToDto(job));
    }

    [HttpDelete("jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CancelJob(Guid jobId)
        => jobManager.Cancel(jobId) ? NoContent() : NotFound();

    private static TranslationJobDto ToDto(TranslationJob job)
        => new(
            job.Id,
            job.FromCulture,
            job.Overwrite,
            job.Status.ToString(),
            job.StatusMessage,
            job.CreatedUtc,
            job.CompletedUtc,
            job.Progress,
            job.Messages,
            job.Errors);
}
