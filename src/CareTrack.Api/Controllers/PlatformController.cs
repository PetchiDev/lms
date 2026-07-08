using Asp.Versioning;
using CareTrack.Application.DTOs.Platform;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform")]
[Route("api/{version:apiVersion}/platform")]
public class PlatformController : ControllerBase
{
    private readonly IPlatformService _service;

    public PlatformController(IPlatformService service) => _service = service;

    /// <summary>Returns Apollo platform branding (logo URL).</summary>
    [HttpGet("branding")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlatformBrandingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformBrandingResponse>> GetBranding(CancellationToken cancellationToken)
        => Ok(await _service.GetBrandingAsync(cancellationToken));

    /// <summary>Uploads the Apollo admin platform logo.</summary>
    [HttpPost("logo")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin))]
    [ProducesResponseType(typeof(PlatformBrandingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlatformBrandingResponse>> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("Logo file is required.");

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadLogoAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(result);
    }
}
