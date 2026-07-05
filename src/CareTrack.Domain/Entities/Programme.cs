using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Programme : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationYears { get; set; } = 3;

    public ICollection<ProgrammeYear> Years { get; set; } = [];
    public ICollection<UniversityProgramme> UniversityProgrammes { get; set; } = [];
}
