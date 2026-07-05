using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CareTrack.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? UniversityId { get; set; }
    public Guid? CohortId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? SupervisorId { get; set; }
    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Active;
    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpiry { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
