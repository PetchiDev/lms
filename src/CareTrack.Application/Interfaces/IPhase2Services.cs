using CareTrack.Application.DTOs.Calendar;
using CareTrack.Application.DTOs.Clinical;
using CareTrack.Application.DTOs.Integration;
using CareTrack.Application.DTOs.Notifications;

namespace CareTrack.Application.Interfaces;

public interface IClinicalService
{
    Task<IReadOnlyList<HospitalDepartmentResponse>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<HospitalDepartmentResponse> CreateDepartmentAsync(string name, string code, int capacity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RotationResponse>> GetRotationsAsync(CancellationToken cancellationToken = default);
    Task<RotationResponse> CreateRotationAsync(CreateRotationRequest request, CancellationToken cancellationToken = default);
    Task AssignStudentsAsync(Guid rotationId, AssignStudentsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RotationAssignmentResponse>> GetMyRotationsAsync(CancellationToken cancellationToken = default);
    Task<LogbookEntryResponse> CreateLogbookEntryAsync(CreateLogbookEntryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogbookEntryResponse>> GetMyLogbookEntriesAsync(CancellationToken cancellationToken = default);
    Task<SupervisorDashboardResponse> GetSupervisorDashboardAsync(CancellationToken cancellationToken = default);
    Task<LogbookEntryResponse> SignOffEntryAsync(Guid entryId, SignOffRequest request, CancellationToken cancellationToken = default);
    Task ProcessEscalationsAsync(CancellationToken cancellationToken = default);
}

public interface IIntegrationService
{
    Task RunSisRosterSyncAsync(Guid? universityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SisSyncRunResponse>> GetSisSyncHistoryAsync(CancellationToken cancellationToken = default);
    Task<GradeSyncRequestResponse> CreateGradeSyncRequestAsync(CreateGradeSyncRequest request, CancellationToken cancellationToken = default);
    Task ApproveGradeSyncAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GradeSyncRequestResponse>> GetGradeSyncRequestsAsync(CancellationToken cancellationToken = default);
    Task ProcessHospitalAttendanceFeedAsync(CancellationToken cancellationToken = default);
    Task ReceiveHospitalFeedAsync(Guid universityId, HospitalFeedWebhookRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceFeedStatusResponse> GetAttendanceFeedStatusAsync(CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendAsync(string userId, Guid universityId, string type, string title, string body, string channel, Guid? relatedEntityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task ProcessRemindersAsync(CancellationToken cancellationToken = default);
}

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarEventResponse>> GetMyScheduleAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);
    Task<CalendarEventResponse> CreateEventAsync(CreateCalendarEventRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscussionThreadResponse>> GetLessonThreadsAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<DiscussionThreadResponse> CreateThreadAsync(Guid lessonId, CreateThreadRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscussionPostResponse>> GetThreadPostsAsync(Guid threadId, CancellationToken cancellationToken = default);
    Task<DiscussionPostResponse> CreatePostAsync(Guid threadId, CreatePostRequest request, CancellationToken cancellationToken = default);
}

public interface IContentVersionService
{
    Task<IReadOnlyList<ContentVersionResponse>> GetLessonVersionsAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<ContentVersionResponse> CreateVersionAsync(Guid lessonId, string blobUrl, string? changelog, CancellationToken cancellationToken = default);
    Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task LinkAssessmentVersionAsync(LinkAssessmentVersionRequest request, CancellationToken cancellationToken = default);
}
