using Asp.Versioning;
using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Enrolment;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enrolments")]
[Authorize(Roles = nameof(UserRole.UniversityAdmin) + "," + nameof(UserRole.ApolloAdmin))]
public class EnrolmentsController : ControllerBase
{
    private readonly IEnrolmentService _service;

    public EnrolmentsController(IEnrolmentService service) => _service = service;

    [HttpPost("students")]
    [ProducesResponseType(typeof(StudentEnrolmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<StudentEnrolmentResponse>> CreateStudent([FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateStudentAsync(request, cancellationToken);
        return Created(string.Empty, result);
    }

    [HttpPost("students/import")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CsvImportResult>> ImportStudents(IFormFile file, [FromQuery] Guid cohortId, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("File is required.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".xlsx")
            return BadRequest("Only CSV and XLSX files are supported.");

        await using var stream = file.OpenReadStream();
        return Ok(await _service.ImportStudentsAsync(stream, file.FileName, cohortId, cancellationToken));
    }

    [HttpGet("students")]
    [ProducesResponseType(typeof(PagedResult<StudentEnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<StudentEnrolmentResponse>>> GetStudents([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        => Ok(await _service.GetStudentsAsync(page, pageSize, cancellationToken));

    /// <summary>Assign a student to a cohort (and its programme).</summary>
    [HttpPatch("students/{studentId:guid}/cohort")]
    [ProducesResponseType(typeof(StudentEnrolmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentEnrolmentResponse>> AssignCohort(
        Guid studentId,
        [FromBody] AssignStudentCohortRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.AssignStudentCohortAsync(studentId, request, cancellationToken));

    /// <summary>Updates a student's profile, cohort, status, or credentials.</summary>
    [HttpPut("students/{studentId:guid}")]
    [ProducesResponseType(typeof(StudentEnrolmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentEnrolmentResponse>> UpdateStudent(
        Guid studentId,
        [FromBody] UpdateStudentRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.UpdateStudentAsync(studentId, request, cancellationToken));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cohorts")]
[Authorize(Roles = nameof(UserRole.UniversityAdmin) + "," + nameof(UserRole.ApolloAdmin))]
public class CohortsController : ControllerBase
{
    private readonly IEnrolmentService _service;

    public CohortsController(IEnrolmentService service) => _service = service;

    [HttpPost]
    [ProducesResponseType(typeof(CohortResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CohortResponse>> Create([FromBody] CreateCohortRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateCohortAsync(request, cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CohortResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CohortResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetCohortsAsync(cancellationToken));
}
