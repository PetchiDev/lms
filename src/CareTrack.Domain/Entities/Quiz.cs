using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Quiz : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PassPercentage { get; set; } = 60;
    public int TimeLimitMinutes { get; set; } = 30;
    public int MaxAttempts { get; set; } = 3;
    public int CooldownHours { get; set; } = 24;
    public bool IsActive { get; set; } = true;

    public Module Module { get; set; } = null!;
    public ICollection<QuizQuestion> Questions { get; set; } = [];
    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}
