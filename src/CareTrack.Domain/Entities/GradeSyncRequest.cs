using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class GradeSyncRequest : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid CohortId { get; set; }
    public GradeSyncStatus Status { get; set; } = GradeSyncStatus.PendingApproval;
    public string RequestedByUserId { get; set; } = string.Empty;
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PushedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }

    public University University { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public Cohort Cohort { get; set; } = null!;
    public ICollection<GradeSyncRecord> Records { get; set; } = [];
}
