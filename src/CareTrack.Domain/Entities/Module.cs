using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Module : BaseEntity
{
    public Guid SemesterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Semester Semester { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = [];
    public ICollection<ModulePrerequisite> Prerequisites { get; set; } = [];
    public ICollection<ModulePrerequisite> RequiredBy { get; set; } = [];
    public ICollection<Quiz> Quizzes { get; set; } = [];
    public ICollection<ModuleProgress> ModuleProgresses { get; set; } = [];
}
