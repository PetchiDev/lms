using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class SisRosterSyncRun : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public SyncRunStatus Status { get; set; } = SyncRunStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsAdded { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSuspended { get; set; }
    public string? ErrorMessage { get; set; }

    public University University { get; set; } = null!;
    public ICollection<SisRosterSyncRecord> Records { get; set; } = [];
}
