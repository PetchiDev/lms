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

    /// <summary>Deletes a university (only when no active data depends on it).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes all partner universities (skips those with blocking dependencies).</summary>
    [HttpDelete]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(DeleteAllUniversitiesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeleteAllUniversitiesResponse>> DeleteAll(CancellationToken cancellationToken)
        => Ok(await _service.DeleteAllAsync(cancellationToken));

    /// <summary>Uploads the partner university logo (stored in Azure Blob).</summary>
    [HttpPost("{id:guid}/logo")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(UniversityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UniversityResponse>> UploadLogo(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Logo file is required.");

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadLogoAsync(id, stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets the university-specific invite email template.</summary>
    [HttpGet("{id:guid}/email-template")]
    [ProducesResponseType(typeof(UniversityEmailTemplateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniversityEmailTemplateResponse>> GetEmailTemplate(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetEmailTemplateAsync(id, cancellationToken));

    /// <summary>Updates the university-specific invite email template.</summary>
    [HttpPut("{id:guid}/email-template")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.UniversityAdmin))]
    [ProducesResponseType(typeof(UniversityEmailTemplateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniversityEmailTemplateResponse>> UpdateEmailTemplate(
        Guid id,
        [FromBody] UpdateUniversityEmailTemplateRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.UpdateEmailTemplateAsync(id, request, cancellationToken));
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

    /// <summary>Lists college admins for a partner university.</summary>
    [HttpGet("university-admins")]
    [ProducesResponseType(typeof(IReadOnlyList<UniversityAdminResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UniversityAdminResponse>>> GetUniversityAdmins(
        [FromQuery] Guid universityId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetUniversityAdminsAsync(universityId, cancellationToken));

    /// <summary>Updates a college admin's credentials.</summary>
    [HttpPut("university-admins/{userId}")]
    [ProducesResponseType(typeof(UniversityAdminResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UniversityAdminResponse>> UpdateUniversityAdmin(
        string userId,
        [FromBody] UpdateUniversityAdminRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(userId, request.UserId, StringComparison.Ordinal))
            return BadRequest("User id mismatch.");

        return Ok(await _service.UpdateUniversityAdminAsync(request, cancellationToken));
    }
}
