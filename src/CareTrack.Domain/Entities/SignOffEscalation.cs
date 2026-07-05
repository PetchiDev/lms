using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class SignOffEscalation : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid LogbookEntryId { get; set; }
    public string EscalatedToUserId { get; set; } = string.Empty;
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }

    public University University { get; set; } = null!;
    public LogbookEntry LogbookEntry { get; set; } = null!;
}
