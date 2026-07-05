namespace CareTrack.Application.DTOs.Universities;

public record CreateUniversityRequest(string Name, string Domain, Guid? ProgrammeId);

public record UpdateUniversityRequest(string Name, string Domain, bool IsActive);

public record UniversityResponse(
    Guid Id,
    string Name,
    string Domain,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<Guid> ProgrammeIds);

public record CreateUniversityAdminRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid UniversityId);
