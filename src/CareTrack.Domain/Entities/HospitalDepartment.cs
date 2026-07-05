using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class HospitalDepartment : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CapacityPerMonth { get; set; } = 40;
    public bool IsActive { get; set; } = true;

    public University University { get; set; } = null!;
    public ICollection<Supervisor> Supervisors { get; set; } = [];
    public ICollection<Rotation> Rotations { get; set; } = [];
}
