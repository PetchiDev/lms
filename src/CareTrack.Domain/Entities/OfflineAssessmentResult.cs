using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class OfflineAssessmentResult : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ModuleId { get; set; }
    public string AssessmentType { get; set; } = string.Empty;
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }
    public string? Notes { get; set; }
    public string EnteredByUserId { get; set; } = string.Empty;

    public Student Student { get; set; } = null!;
    public Module Module { get; set; } = null!;
}
