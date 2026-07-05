using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class GradeSyncRecord : BaseEntity
{
    public Guid GradeSyncRequestId { get; set; }
    public Guid StudentId { get; set; }
    public string MarksJson { get; set; } = "{}";
    public string SyncStatus { get; set; } = "Pending";
    public string? ExternalReference { get; set; }

    public GradeSyncRequest GradeSyncRequest { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
