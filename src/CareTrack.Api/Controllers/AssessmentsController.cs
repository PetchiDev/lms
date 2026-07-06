using Asp.Versioning;
using CareTrack.Application.DTOs.Assessment;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assessments")]
[Authorize]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _service;

    public AssessmentsController(IAssessmentService service) => _service = service;

    [HttpGet("programmes/{programmeId:guid}/overview")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
    [ProducesResponseType(typeof(ProgrammeAssessmentOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgrammeAssessmentOverviewResponse>> GetProgrammeOverview(Guid programmeId, CancellationToken cancellationToken)
        => Ok(await _service.GetProgrammeOverviewAsync(programmeId, cancellationToken));

    [HttpGet("modules/{moduleId:guid}")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
    [ProducesResponseType(typeof(AdminQuizDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminQuizDetailResponse>> GetModuleQuiz(Guid moduleId, CancellationToken cancellationToken)
        => Ok(await _service.GetAdminQuizAsync(moduleId, cancellationToken));

    [HttpPut("modules/{moduleId:guid}")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
    [ProducesResponseType(typeof(AdminQuizDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminQuizDetailResponse>> UpsertModuleQuiz(
        Guid moduleId,
        [FromBody] UpsertQuizRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.UpsertQuizAsync(moduleId, request, cancellationToken));

    [HttpPost("offline-results")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty) + "," + nameof(UserRole.UniversityAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordOfflineResult([FromBody] OfflineAssessmentRequest request, CancellationToken cancellationToken)
    {
        await _service.RecordOfflineResultAsync(request, cancellationToken);
        return NoContent();
    }
}
