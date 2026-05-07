using Asp.Versioning;
using Diplo.Translator.Models;
using Diplo.Translator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;
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
    private readonly IBackOfficeSecurityAccessor backOfficeSecurityAccessor;
    private readonly TranslatorConfig config;

    public TranslationApiController(
        IDictionaryTranslationService dictionaryTranslationService,
        ITranslationJobManager jobManager,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IOptions<TranslatorConfig> config)
    {
        this.dictionaryTranslationService = dictionaryTranslationService;
        this.jobManager = jobManager;
        this.backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        this.config = config.Value;
    }

    [HttpGet("languages")]
    [ProducesResponseType<IEnumerable<TranslatorLanguageDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TranslatorLanguageDto>>> GetLanguages()
        => Ok((await dictionaryTranslationService.GetAllLanguagesAsync())
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TranslationJobCreatedDto>> StartJob([FromBody] TranslationJobRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FromCulture))
        {
            return BadRequest("A source culture is required.");
        }

        var language = (await dictionaryTranslationService.GetAllLanguagesAsync())
            .FirstOrDefault(x => x.IsoCode.InvariantEquals(request.FromCulture));

        if (language is null)
        {
            return BadRequest($"Language '{request.FromCulture}' was not found.");
        }

        var userKey = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;
        if (userKey is null)
        {
            return Unauthorized();
        }

        if (!jobManager.TryStart(request, userKey.Value, out var job))
        {
            return Conflict("A dictionary translation job is already running.");
        }

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
