using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class Supervisor : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid HospitalDepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public University University { get; set; } = null!;
    public HospitalDepartment HospitalDepartment { get; set; } = null!;
    public ICollection<LogbookEntry> ReviewedEntries { get; set; } = [];
    public ICollection<SupervisorDelegation> DelegationsGiven { get; set; } = [];
    public ICollection<SupervisorDelegation> DelegationsReceived { get; set; } = [];
}
