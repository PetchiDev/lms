using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class ModulePrerequisite : BaseEntity
{
    public Guid ModuleId { get; set; }
    public Guid PrerequisiteModuleId { get; set; }

    public Module Module { get; set; } = null!;
    public Module PrerequisiteModule { get; set; } = null!;
}
