using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class SupervisorDelegation : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid SupervisorId { get; set; }
    public Guid DelegateSupervisorId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public University University { get; set; } = null!;
    public Supervisor Supervisor { get; set; } = null!;
    public Supervisor DelegateSupervisor { get; set; } = null!;
}
