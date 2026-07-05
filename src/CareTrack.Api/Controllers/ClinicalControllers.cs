using Asp.Versioning;
using CareTrack.Application.DTOs.Calendar;
using CareTrack.Application.DTOs.Clinical;
using CareTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rotations")]
[ApiVersion("1.0")]
[Authorize]
public class RotationsController : ControllerBase
{
    private readonly IClinicalService _clinical;

    public RotationsController(IClinicalService clinical) => _clinical = clinical;

    /// <summary>Lists clinical rotations for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RotationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RotationResponse>>> GetRotations(CancellationToken cancellationToken)
        => Ok(await _clinical.GetRotationsAsync(cancellationToken));

    /// <summary>Creates a rotation posting (Apollo coordinator).</summary>
    [HttpPost]
    [Authorize(Roles = "ApolloAdmin,ApolloFaculty")]
    [ProducesResponseType(typeof(RotationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RotationResponse>> CreateRotation([FromBody] CreateRotationRequest request, CancellationToken cancellationToken)
    {
        var result = await _clinical.CreateRotationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRotations), result);
    }

    /// <summary>Assigns students to a rotation.</summary>
    [HttpPost("{rotationId:guid}/assignments")]
    [Authorize(Roles = "ApolloAdmin,ApolloFaculty")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignStudents(Guid rotationId, [FromBody] AssignStudentsRequest request, CancellationToken cancellationToken)
    {
        await _clinical.AssignStudentsAsync(rotationId, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Lists hospital departments.</summary>
    [HttpGet("departments")]
    [ProducesResponseType(typeof(IReadOnlyList<HospitalDepartmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HospitalDepartmentResponse>>> GetDepartments(CancellationToken cancellationToken)
        => Ok(await _clinical.GetDepartmentsAsync(cancellationToken));

    /// <summary>Creates a hospital department.</summary>
    [HttpPost("departments")]
    [Authorize(Roles = "ApolloAdmin,ApolloFaculty")]
    [ProducesResponseType(typeof(HospitalDepartmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<HospitalDepartmentResponse>> CreateDepartment([FromQuery] string name, [FromQuery] string code, [FromQuery] int capacity = 40, CancellationToken cancellationToken = default)
        => Ok(await _clinical.CreateDepartmentAsync(name, code, capacity, cancellationToken));
}

[ApiController]
[Route("api/v{version:apiVersion}/signoffs")]
[ApiVersion("1.0")]
[Authorize(Roles = "Supervisor")]
public class SignoffsController : ControllerBase
{
    private readonly IClinicalService _clinical;

    public SignoffsController(IClinicalService clinical) => _clinical = clinical;

    /// <summary>Supervisor dashboard with pending logbook entries.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(SupervisorDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupervisorDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _clinical.GetSupervisorDashboardAsync(cancellationToken));

    /// <summary>Approve or reject a logbook entry.</summary>
    [HttpPost("{entryId:guid}")]
    [ProducesResponseType(typeof(LogbookEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LogbookEntryResponse>> SignOff(Guid entryId, [FromBody] SignOffRequest request, CancellationToken cancellationToken)
        => Ok(await _clinical.SignOffEntryAsync(entryId, request, cancellationToken));
}
