using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class University : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
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
