using Asp.Versioning;
using CareTrack.Application.DTOs.Programmes;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[Route("api/programmes")]
[Authorize]
public class ProgrammesController : ControllerBase
{
    private readonly IProgrammeService _service;

    public ProgrammesController(IProgrammeService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(ProgrammeResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProgrammeResponse>> Create([FromBody] CreateProgrammeRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStructure), new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProgrammeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProgrammeResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("catalogue")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty))]
    [ProducesResponseType(typeof(ProgrammeCatalogueResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgrammeCatalogueResponse>> GetCatalogue(CancellationToken cancellationToken)
        => Ok(await _service.GetCatalogueAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProgrammeStructureResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgrammeStructureResponse>> GetStructure(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetStructureAsync(id, cancellationToken));

    [HttpPost("{id:guid}/years")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(ProgrammeYearResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProgrammeYearResponse>> AddYear(Guid id, [FromBody] CreateProgrammeYearRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AddYearAsync(id, request, cancellationToken));

    [HttpPost("years/{yearId:guid}/semesters")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(SemesterResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SemesterResponse>> AddSemester(Guid yearId, [FromBody] CreateSemesterRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AddSemesterAsync(yearId, request, cancellationToken));

    [HttpPost("semesters/{semesterId:guid}/modules")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(ModuleSummaryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ModuleSummaryResponse>> AddModule(Guid semesterId, [FromBody] CreateModuleRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AddModuleAsync(semesterId, request, cancellationToken));

    /// <summary>Deletes a programme and its full structure (years/semesters/modules/content).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a module and its lessons, assets, and assessments.</summary>
    [HttpDelete("modules/{moduleId:guid}")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteModule(Guid moduleId, CancellationToken cancellationToken)
    {
        await _service.DeleteModuleAsync(moduleId, cancellationToken);
        return NoContent();
    }
}
