using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class DiscussionThread : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public bool IsLocked { get; set; }

    public University University { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
    public ICollection<DiscussionPost> Posts { get; set; } = [];
}
