using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class SisRosterSyncRecord : BaseEntity
{
    public Guid SyncRunId { get; set; }
    public string ExternalStudentId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public DateTime? AppliedAt { get; set; }
    public string? Details { get; set; }

    public SisRosterSyncRun SyncRun { get; set; } = null!;
}
