using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

/// <summary>
/// When UniversityId is null, lesson is published to all universities.
/// </summary>
public class ContentPublication : BaseEntity
{
    public Guid LessonId { get; set; }
    public Guid? UniversityId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public string? PublishedByUserId { get; set; }

    public Lesson Lesson { get; set; } = null!;
    public University? University { get; set; }
}
