using CareTrack.Domain.Common;
using CareTrack.Domain.Enums;

namespace CareTrack.Domain.Entities;

public class CalendarEvent : BaseEntity, ITenantEntity
{
    public Guid UniversityId { get; set; }
    public Guid? CohortId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public CalendarEventType EventType { get; set; } = CalendarEventType.LiveClass;
    public Guid? RelatedModuleId { get; set; }
    public Guid? RelatedRotationId { get; set; }

    public University University { get; set; } = null!;
    public Cohort? Cohort { get; set; }
    public LiveClassSession? LiveClassSession { get; set; }
}
