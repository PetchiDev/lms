using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class LessonAsset : BaseEntity
{
    public Guid LessonId { get; set; }
    public AssetType AssetType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public Lesson Lesson { get; set; } = null!;
}
