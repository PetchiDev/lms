using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class LiveClassSession : BaseEntity
{
    public Guid CalendarEventId { get; set; }
    public string JoinUrl { get; set; } = string.Empty;
    public int MinAttendanceMinutes { get; set; } = 45;
    public string? RecordingUrl { get; set; }

    public CalendarEvent CalendarEvent { get; set; } = null!;
}
