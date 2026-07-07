using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class University : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? EmailInviteSubject { get; set; }
    public string? EmailInviteBodyHtml { get; set; }
    public string? EmailFromName { get; set; }
    public string? EmailFromEmail { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Cohort> Cohorts { get; set; } = [];
    public ICollection<StudentEnrolment> Enrolments { get; set; } = [];
    public ICollection<UniversityProgramme> UniversityProgrammes { get; set; } = [];
    public ICollection<ContentPublication> ContentPublications { get; set; } = [];
    public TenantIdpConfig? IdpConfig { get; set; }
    public ICollection<HospitalDepartment> HospitalDepartments { get; set; } = [];
    public ICollection<Supervisor> Supervisors { get; set; } = [];
    public ICollection<Rotation> Rotations { get; set; } = [];
}
