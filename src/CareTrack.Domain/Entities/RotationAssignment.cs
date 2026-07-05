using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class RotationAssignment : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid RotationId { get; set; }
    public Guid StudentId { get; set; }
    public RotationStatus Status { get; set; } = RotationStatus.Scheduled;
    public decimal AttendancePercent { get; set; }
    public int CompletedProcedureCount { get; set; }
    public DateTime? CompletedAt { get; set; }

    public University University { get; set; } = null!;
    public Rotation Rotation { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<LogbookEntry> LogbookEntries { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}
