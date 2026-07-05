using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Cohort : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid ProgrammeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int IntakeYear { get; set; }
    public int CurrentYear { get; set; } = 1;
    public int CurrentSemester { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public University University { get; set; } = null!;
    public Programme Programme { get; set; } = null!;
    public ICollection<StudentEnrolment> Enrolments { get; set; } = [];
}
