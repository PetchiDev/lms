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

    [HttpPost("offline-results")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty) + "," + nameof(UserRole.UniversityAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordOfflineResult([FromBody] OfflineAssessmentRequest request, CancellationToken cancellationToken)
    {
        await _service.RecordOfflineResultAsync(request, cancellationToken);
        return NoContent();
    }
}
