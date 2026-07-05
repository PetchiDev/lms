using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class UniversityProgramme : BaseEntity
{
    public Guid UniversityId { get; set; }
    public Guid ProgrammeId { get; set; }

    public University University { get; set; } = null!;
    public Programme Programme { get; set; } = null!;
}
