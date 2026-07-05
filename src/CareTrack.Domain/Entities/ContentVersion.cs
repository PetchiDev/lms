using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class ContentVersion : BaseEntity
{
    public Guid LessonId { get; set; }
    public int VersionNumber { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string? Changelog { get; set; }
    public ContentVersionStatus Status { get; set; } = ContentVersionStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? CreatedByUserId { get; set; }

    public Lesson Lesson { get; set; } = null!;
    public ICollection<AssessmentContentVersion> AssessmentLinks { get; set; } = [];
}
