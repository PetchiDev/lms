using System.Text.Json;
using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Platform;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;

namespace CareTrack.Infrastructure.Services;

public class PlatformService : IPlatformService
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ITenantContext _tenant;
    private readonly string _settingsPath;

    public PlatformService(IBlobStorageService blobStorage, ITenantContext tenant)
    {
        _blobStorage = blobStorage;
        _tenant = tenant;
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "platform");
        Directory.CreateDirectory(basePath);
        _settingsPath = Path.Combine(basePath, "branding.json");
    }

    public async Task<PlatformBrandingResponse> GetBrandingAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
            return new PlatformBrandingResponse(null);

        await using var stream = File.OpenRead(_settingsPath);
        var branding = await JsonSerializer.DeserializeAsync<PlatformBrandingResponse>(stream, cancellationToken: cancellationToken);
        return branding ?? new PlatformBrandingResponse(null);
    }

    public async Task<PlatformBrandingResponse> UploadLogoAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can update platform branding.");

        var existing = await GetBrandingAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(existing.LogoUrl))
            await _blobStorage.DeleteAsync(existing.LogoUrl, cancellationToken);

        var logoUrl = await _blobStorage.UploadAsync(fileStream, fileName, contentType, "media/platform", cancellationToken);
        var response = new PlatformBrandingResponse(logoUrl);

        await using var writeStream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(writeStream, response, cancellationToken: cancellationToken);

        return response;
    }
}
