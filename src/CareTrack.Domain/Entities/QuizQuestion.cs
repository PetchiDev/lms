using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Points { get; set; } = 1;

    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizOption> Options { get; set; } = [];
}
