using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid StudentId { get; set; }
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int AttemptNumber { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<QuizAnswer> Answers { get; set; } = [];
}
