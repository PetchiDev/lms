using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class AssessmentContentVersion : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid ContentVersionId { get; set; }
    public string? Notes { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ContentVersion ContentVersion { get; set; } = null!;
}
