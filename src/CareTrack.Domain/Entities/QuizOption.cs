using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class QuizOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}
