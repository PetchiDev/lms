using Asp.Versioning;
using CareTrack.Application.DTOs.Reports;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _service;

    public ReportsController(IReportingService service) => _service = service;

    [HttpGet("university/students")]
    [Authorize(Roles = nameof(UserRole.UniversityAdmin) + "," + nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(CohortReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CohortReportResponse>> GetUniversityStudents([FromQuery] Guid? cohortId, CancellationToken cancellationToken)
        => Ok(await _service.GetUniversityStudentReportAsync(cohortId, cancellationToken));

    [HttpGet("university/export")]
    [Authorize(Roles = nameof(UserRole.UniversityAdmin) + "," + nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportUniversityReport([FromQuery] string format = "excel", [FromQuery] Guid? cohortId = null, CancellationToken cancellationToken = default)
    {
        var bytes = await _service.ExportReportAsync(new ExportReportRequest(format, cohortId), cancellationToken);
        var contentType = format.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var fileName = format.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? "report.pdf" : "report.xlsx";
        return File(bytes, contentType, fileName);
    }

    [HttpGet("apollo/universities")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
    [ProducesResponseType(typeof(IReadOnlyList<UniversityComparisonReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UniversityComparisonReport>>> GetApolloUniversities(CancellationToken cancellationToken)
        => Ok(await _service.GetApolloUniversityReportAsync(cancellationToken));

    [HttpGet("apollo/content-performance")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
    [ProducesResponseType(typeof(IReadOnlyList<ContentPerformanceReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentPerformanceReport>>> GetContentPerformance(CancellationToken cancellationToken)
        => Ok(await _service.GetContentPerformanceAsync(cancellationToken));
}
