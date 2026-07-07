namespace CareTrack.Application.DTOs.Universities;

public record CreateUniversityRequest(string Name, string Domain, Guid? ProgrammeId);

public record UpdateUniversityRequest(string Name, string Domain, bool IsActive);

public record SetUniversityProgrammesRequest(IReadOnlyList<Guid> ProgrammeIds);

public record UniversityResponse(
    Guid Id,
    string Name,
    string Domain,
    string? LogoUrl,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<Guid> ProgrammeIds,
    bool HasCustomEmailTemplate);

public record UniversityEmailTemplateResponse(
    string? EmailInviteSubject,
    string? EmailInviteBodyHtml,
    string? EmailFromName,
    string? EmailFromEmail);

public record UpdateUniversityEmailTemplateRequest(
    string? EmailInviteSubject,
    string? EmailInviteBodyHtml,
    string? EmailFromName,
    string? EmailFromEmail);

public record CreateUniversityAdminRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid UniversityId);

public record CreateUniversityAdminDirectRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid UniversityId,
    string Password);

public record UniversityAdminResponse(
    string UserId,
    string Email,
    string FullName,
    Guid UniversityId);
