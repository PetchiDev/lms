using Asp.Versioning;
using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Universities;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/universities")]
[Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.UniversityAdmin))]
public class UniversitiesController : ControllerBase
{
    private readonly IUniversityService _service;

    public UniversitiesController(IUniversityService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UniversityResponse>> Create([FromBody] CreateUniversityRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniversityResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UniversityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UniversityResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _service.GetAllAsync(page, pageSize, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniversityResponse>> Update(Guid id, [FromBody] UpdateUniversityRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Link programmes to a partner university.</summary>
    [HttpPut("{id:guid}/programmes")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniversityResponse>> SetProgrammes(
        Guid id,
        [FromBody] SetUniversityProgrammesRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.SetProgrammesAsync(id, request, cancellationToken));
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = nameof(UserRole.ApolloAdmin))]
public class UsersController : ControllerBase
{
    private readonly IUniversityService _service;

    public UsersController(IUniversityService service) => _service = service;

    [HttpPost("university-admins")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> InviteUniversityAdmin([FromBody] CreateUniversityAdminRequest request, CancellationToken cancellationToken)
    {
        await _service.InviteUniversityAdminAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Creates an active university admin with password (no invite email).</summary>
    [HttpPost("university-admins/direct")]
    [ProducesResponseType(typeof(UniversityAdminResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UniversityAdminResponse>> CreateUniversityAdmin(
        [FromBody] CreateUniversityAdminDirectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateUniversityAdminAsync(request, cancellationToken);
        return Created(string.Empty, result);
    }
}
