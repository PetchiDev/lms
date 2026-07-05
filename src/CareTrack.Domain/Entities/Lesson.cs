using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class Lesson : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public int SortOrder { get; set; }
    public string? CreatedByUserId { get; set; }
    public int? DurationSeconds { get; set; }

    public Module Module { get; set; } = null!;
    public ICollection<LessonAsset> Assets { get; set; } = [];
    public ICollection<ContentPublication> Publications { get; set; } = [];
    public ICollection<LessonProgress> ProgressRecords { get; set; } = [];
    public ICollection<ContentVersion> ContentVersions { get; set; } = [];
    public ICollection<DiscussionThread> DiscussionThreads { get; set; } = [];
}
