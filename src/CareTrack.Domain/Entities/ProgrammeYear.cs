using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class ProgrammeYear : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public int YearNumber { get; set; }
    public string Name { get; set; } = string.Empty;

    public Programme Programme { get; set; } = null!;
    public ICollection<Semester> Semesters { get; set; } = [];
}
