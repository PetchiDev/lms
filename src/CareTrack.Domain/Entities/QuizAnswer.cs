using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class QuizAnswer : BaseEntity
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }

    public QuizAttempt Attempt { get; set; } = null!;
    public QuizQuestion Question { get; set; } = null!;
    public QuizOption SelectedOption { get; set; } = null!;
}
