namespace CareTrack.Application.Interfaces;

public interface IEmailService
{
    Task SendInviteEmailAsync(string email, string fullName, string activationToken, Guid? universityId = null, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string email, string subject, string body, string? fromEmail = null, string? fromName = null, CancellationToken cancellationToken = default);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string folder = "media", CancellationToken cancellationToken = default);
    Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
    bool UsesAzureBlob { get; }
}

public interface IJwtTokenService
{
    string GenerateToken(string userId, string email, string role, Guid? universityId, Guid? cohortId, Guid? studentId, Guid? supervisorId = null);
}
