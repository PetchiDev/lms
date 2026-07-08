using Asp.Versioning;
using CareTrack.Application.DTOs.Content;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/content")]
[Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
public class ContentController : ControllerBase
{
    private readonly IContentService _service;

    public ContentController(IContentService service) => _service = service;

    [HttpGet("modules")]
    [ProducesResponseType(typeof(IReadOnlyList<ModulePickerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModulePickerResponse>>> GetModules(CancellationToken cancellationToken)
        => Ok(await _service.GetModulesAsync(cancellationToken));

    [HttpGet("modules/{moduleId:guid}/lessons")]
    [ProducesResponseType(typeof(IReadOnlyList<LessonListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LessonListItemResponse>>> GetModuleLessons(Guid moduleId, CancellationToken cancellationToken)
        => Ok(await _service.GetModuleLessonsAsync(moduleId, cancellationToken));

    [HttpPost("programmes/map")]
    [ProducesResponseType(typeof(MapProgrammesToUniversitiesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MapProgrammesToUniversitiesResponse>> MapProgrammesToUniversities(
        [FromBody] MapProgrammesToUniversitiesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.MapProgrammesToUniversitiesAsync(request, cancellationToken));

    [HttpPost("modules/{moduleId:guid}/publish")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> PublishModule(Guid moduleId, [FromBody] PublishModuleRequest request, CancellationToken cancellationToken)
    {
        var count = await _service.PublishModuleAsync(moduleId, request, cancellationToken);
        return Ok(new { lessonsPublished = count });
    }

    [HttpPost("lessons")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LessonResponse>> CreateLesson([FromBody] CreateLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateLessonAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetLesson), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpGet("lessons/{id:guid}")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonResponse>> GetLesson(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetLessonAsync(id, cancellationToken));

    [HttpPut("lessons/{id:guid}")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonResponse>> UpdateLesson(Guid id, [FromBody] UpdateLessonRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateLessonAsync(id, request, cancellationToken));

    [HttpPost("lessons/{id:guid}/assets")]
    [ProducesResponseType(typeof(LessonAssetResponse), StatusCodes.Status201Created)]
    [RequestSizeLimit(524_288_000)]
    public async Task<ActionResult<LessonAssetResponse>> UploadAsset(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _service.UploadAssetAsync(id, stream, file.FileName, file.ContentType, cancellationToken);
        return Created(string.Empty, result);
    }

    [HttpDelete("lessons/{lessonId:guid}/assets/{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsset(Guid lessonId, Guid assetId, CancellationToken cancellationToken)
    {
        await _service.DeleteAssetAsync(lessonId, assetId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("lessons/{id:guid}/status")]
    [ProducesResponseType(typeof(LessonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonResponse>> UpdateStatus(Guid id, [FromBody] UpdateLessonStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStatusAsync(id, request, cancellationToken));

    [HttpPost("lessons/{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishLessonRequest request, CancellationToken cancellationToken)
    {
        await _service.PublishAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("lessons/{id:guid}/review")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveReview(Guid id, CancellationToken cancellationToken)
    {
        await _service.ApproveReviewAsync(id, cancellationToken);
        return NoContent();
    }
}
