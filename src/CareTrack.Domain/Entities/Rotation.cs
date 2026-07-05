using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class Rotation : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid HospitalDepartmentId { get; set; }
    public Guid CohortId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int WeeksDuration { get; set; } = 6;
    public int RequiredProcedureCount { get; set; } = 10;
    public decimal RequiredAttendancePercent { get; set; } = 85m;
    public RotationStatus Status { get; set; } = RotationStatus.Scheduled;

    public University University { get; set; } = null!;
    public HospitalDepartment HospitalDepartment { get; set; } = null!;
    public Cohort Cohort { get; set; } = null!;
    public ICollection<RotationAssignment> Assignments { get; set; } = [];
}
