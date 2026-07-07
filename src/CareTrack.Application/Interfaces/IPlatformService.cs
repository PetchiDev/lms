using CareTrack.Application.DTOs.Platform;

namespace CareTrack.Application.Interfaces;

public interface IPlatformService
{
    Task<PlatformBrandingResponse> GetBrandingAsync(CancellationToken cancellationToken = default);
    Task<PlatformBrandingResponse> UploadLogoAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
