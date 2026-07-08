using Asp.Versioning;
using CareTrack.Application.DTOs.Certificates;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateService _service;
    private readonly IBlobStorageService _blobStorage;

    public CertificatesController(ICertificateService service, IBlobStorageService blobStorage)
    {
        _service = service;
        _blobStorage = blobStorage;
    }

    /// <summary>Gets the platform certificate template configuration.</summary>
    [HttpGet("template")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.ApolloFaculty) + "," + nameof(UserRole.UniversityAdmin))]
    [ProducesResponseType(typeof(CertificateTemplateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CertificateTemplateResponse>> GetTemplate(CancellationToken cancellationToken)
        => Ok(await _service.GetTemplateAsync(cancellationToken));

    /// <summary>Updates the certificate template (Apollo admin only).</summary>
    [HttpPut("template")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.UniversityAdmin))]
    [ProducesResponseType(typeof(CertificateTemplateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CertificateTemplateResponse>> UpdateTemplate(
        [FromBody] UpdateCertificateTemplateRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.UpdateTemplateAsync(request, cancellationToken));

    /// <summary>Uploads an image asset for certificate template (logo or signature).</summary>
    [HttpPost("template/assets")]
    [Authorize(Roles = nameof(UserRole.ApolloAdmin) + "," + nameof(UserRole.UniversityAdmin))]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> UploadAsset(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("File is required.");

        var universityId = User.FindFirst("universityId")?.Value;
        var folder = User.IsInRole(nameof(UserRole.UniversityAdmin))
            ? $"media/certificates/{universityId ?? "tenant"}"
            : "media/certificates/platform";

        await using var stream = file.OpenReadStream();
        var url = await _blobStorage.UploadAsync(stream, file.FileName, file.ContentType, folder, cancellationToken);
        return Ok(new { url });
    }
}
