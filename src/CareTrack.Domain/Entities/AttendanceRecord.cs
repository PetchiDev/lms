using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class AttendanceRecord : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid RotationAssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public DateOnly RecordDate { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;
    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public int DurationMinutes { get; set; }
    public FeedProcessingStatus FeedStatus { get; set; } = FeedProcessingStatus.Processed;

    public University University { get; set; } = null!;
    public RotationAssignment RotationAssignment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
