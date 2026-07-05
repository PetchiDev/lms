using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class LessonProgress : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid LessonId { get; set; }
    public LessonProgressStatus Status { get; set; } = LessonProgressStatus.NotStarted;
    public int ProgressPercent { get; set; }
    public int WatchedSeconds { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public Student Student { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
