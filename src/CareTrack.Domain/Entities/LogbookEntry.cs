using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class LogbookEntry : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid RotationAssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? SupervisorId { get; set; }
    public DateOnly EntryDate { get; set; }
    public string Procedure { get; set; } = string.Empty;
    public int PatientCount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public LogbookEntryStatus Status { get; set; } = LogbookEntryStatus.PendingSignoff;
    public string? SupervisorRemarks { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }

    public University University { get; set; } = null!;
    public RotationAssignment RotationAssignment { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Supervisor? Supervisor { get; set; }
    public SignOffEscalation? Escalation { get; set; }
}
