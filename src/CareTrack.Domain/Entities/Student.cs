using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Student : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public ICollection<StudentEnrolment> Enrolments { get; set; } = [];
    public ICollection<LessonProgress> LessonProgresses { get; set; } = [];
    public ICollection<ModuleProgress> ModuleProgresses { get; set; } = [];
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = [];
    public ICollection<Certificate> Certificates { get; set; } = [];
}
