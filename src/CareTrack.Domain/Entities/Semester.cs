using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Semester : BaseEntity
{
    public Guid ProgrammeYearId { get; set; }
    public int SemesterNumber { get; set; }
    public string Name { get; set; } = string.Empty;

    public ProgrammeYear ProgrammeYear { get; set; } = null!;
    public ICollection<Module> Modules { get; set; } = [];
}
