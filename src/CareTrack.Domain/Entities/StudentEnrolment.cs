using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class StudentEnrolment : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CohortId { get; set; }
    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Invited;
    public DateTime? ActivatedAt { get; set; }
    public int CurrentYear { get; set; } = 1;
    public int CurrentSemester { get; set; } = 1;

    public University University { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Cohort Cohort { get; set; } = null!;
}
