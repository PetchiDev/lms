namespace CareTrack.Application.Interfaces;

public interface IEmailService
{
    Task SendInviteEmailAsync(string email, string fullName, string activationToken, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    string GenerateToken(string userId, string email, string role, Guid? universityId, Guid? cohortId, Guid? studentId, Guid? supervisorId = null);
}
