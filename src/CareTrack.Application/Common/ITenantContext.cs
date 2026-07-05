using CareTrack.Domain.Enums;

namespace CareTrack.Application.Common;

public interface ITenantContext
{
    string? UserId { get; }
    UserRole Role { get; }
    Guid? UniversityId { get; }
    Guid? CohortId { get; }
    Guid? StudentId { get; }
    Guid? SupervisorId { get; }
    bool IsApolloUser { get; }
    void EnsureUniversityAccess(Guid universityId);
}
