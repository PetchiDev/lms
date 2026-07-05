namespace CareTrack.Application.DTOs.Integration;

public record SisSyncRunResponse(Guid Id, string Status, DateTime StartedAt, DateTime? CompletedAt, int RecordsProcessed, string? ErrorMessage);
public record GradeSyncRequestResponse(Guid Id, string Status, string CohortName, string SemesterName, DateTime CreatedAt, DateTime? ApprovedAt);
public record CreateGradeSyncRequest(Guid SemesterId, Guid CohortId);
public record AttendanceFeedStatusResponse(string Status, int PendingCount, int DelayedCount, DateTime? LastProcessedAt);
public record HospitalFeedWebhookRequest(string ExternalRecordId, string PayloadJson);
