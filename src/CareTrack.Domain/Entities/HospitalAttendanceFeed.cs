using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class HospitalAttendanceFeed : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public string ExternalRecordId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public FeedProcessingStatus Status { get; set; } = FeedProcessingStatus.Received;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public University University { get; set; } = null!;
}
