using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class ModuleProgress : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ModuleId { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Module Module { get; set; } = null!;
}
