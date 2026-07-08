using Asp.Versioning;
using CareTrack.Application.DTOs.Calendar;
using CareTrack.Application.DTOs.Integration;
using CareTrack.Application.DTOs.Notifications;
using CareTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integration;

    public IntegrationsController(IIntegrationService integration) => _integration = integration;

    [HttpPost("sis/roster/sync")]
    [Authorize(Roles = "ApolloAdmin,UniversityAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TriggerSisSync(CancellationToken cancellationToken)
    {
        await _integration.RunSisRosterSyncAsync(cancellationToken: cancellationToken);
        return NoContent();
    }

    [HttpGet("sis/roster/history")]
    [ProducesResponseType(typeof(IReadOnlyList<SisSyncRunResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SisSyncRunResponse>>> GetSisHistory(CancellationToken cancellationToken)
        => Ok(await _integration.GetSisSyncHistoryAsync(cancellationToken));

    [HttpPost("grades/sync")]
    [Authorize(Roles = "UniversityAdmin")]
    [ProducesResponseType(typeof(GradeSyncRequestResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<GradeSyncRequestResponse>> CreateGradeSync([FromBody] CreateGradeSyncRequest request, CancellationToken cancellationToken)
        => Ok(await _integration.CreateGradeSyncRequestAsync(request, cancellationToken));

    [HttpPost("grades/sync/{requestId:guid}/approve")]
    [Authorize(Roles = "UniversityAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApproveGradeSync(Guid requestId, CancellationToken cancellationToken)
    {
        await _integration.ApproveGradeSyncAsync(requestId, cancellationToken);
        return NoContent();
    }

    [HttpGet("grades/sync")]
    [ProducesResponseType(typeof(IReadOnlyList<GradeSyncRequestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GradeSyncRequestResponse>>> GetGradeSyncRequests(CancellationToken cancellationToken)
        => Ok(await _integration.GetGradeSyncRequestsAsync(cancellationToken));

    [HttpPost("attendance/feed/{universityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReceiveAttendanceFeed(Guid universityId, [FromBody] HospitalFeedWebhookRequest request, CancellationToken cancellationToken)
    {
        await _integration.ReceiveHospitalFeedAsync(universityId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("attendance/feed/status")]
    [ProducesResponseType(typeof(AttendanceFeedStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceFeedStatusResponse>> GetFeedStatus(CancellationToken cancellationToken)
        => Ok(await _integration.GetAttendanceFeedStatusAsync(cancellationToken));
}

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetMine(CancellationToken cancellationToken)
        => Ok(await _notifications.GetMyNotificationsAsync(cancellationToken));

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _notifications.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendar;

    public CalendarController(ICalendarService calendar) => _calendar = calendar;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CalendarEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarEventResponse>>> GetSchedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
        => Ok(await _calendar.GetMyScheduleAsync(from, to, cancellationToken));

    [HttpPost]
    [Authorize(Roles = "ApolloAdmin,ApolloFaculty,UniversityAdmin")]
    [ProducesResponseType(typeof(CalendarEventResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CalendarEventResponse>> CreateEvent([FromBody] CreateCalendarEventRequest request, CancellationToken cancellationToken)
        => Ok(await _calendar.CreateEventAsync(request, cancellationToken));
}

[ApiController]
[Route("api/discussions")]
[Authorize]
public class DiscussionsController : ControllerBase
{
    private readonly ICalendarService _calendar;

    public DiscussionsController(ICalendarService calendar) => _calendar = calendar;

    [HttpGet("lessons/{lessonId:guid}/threads")]
    [ProducesResponseType(typeof(IReadOnlyList<DiscussionThreadResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DiscussionThreadResponse>>> GetThreads(Guid lessonId, CancellationToken cancellationToken)
        => Ok(await _calendar.GetLessonThreadsAsync(lessonId, cancellationToken));

    [HttpPost("lessons/{lessonId:guid}/threads")]
    [ProducesResponseType(typeof(DiscussionThreadResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DiscussionThreadResponse>> CreateThread(Guid lessonId, [FromBody] CreateThreadRequest request, CancellationToken cancellationToken)
        => Ok(await _calendar.CreateThreadAsync(lessonId, request, cancellationToken));

    [HttpGet("threads/{threadId:guid}/posts")]
    [ProducesResponseType(typeof(IReadOnlyList<DiscussionPostResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DiscussionPostResponse>>> GetPosts(Guid threadId, CancellationToken cancellationToken)
        => Ok(await _calendar.GetThreadPostsAsync(threadId, cancellationToken));

    [HttpPost("threads/{threadId:guid}/posts")]
    [ProducesResponseType(typeof(DiscussionPostResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DiscussionPostResponse>> CreatePost(Guid threadId, [FromBody] CreatePostRequest request, CancellationToken cancellationToken)
        => Ok(await _calendar.CreatePostAsync(threadId, request, cancellationToken));
}

[ApiController]
[Route("api/content/versions")]
[Authorize(Roles = "ApolloAdmin,ApolloFaculty")]
public class ContentVersionsController : ControllerBase
{
    private readonly IContentVersionService _versions;

    public ContentVersionsController(IContentVersionService versions) => _versions = versions;

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentVersionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentVersionResponse>>> GetVersions(Guid lessonId, CancellationToken cancellationToken)
        => Ok(await _versions.GetLessonVersionsAsync(lessonId, cancellationToken));

    [HttpPost("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(ContentVersionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ContentVersionResponse>> CreateVersion(Guid lessonId, [FromQuery] string blobUrl, [FromQuery] string? changelog, CancellationToken cancellationToken)
        => Ok(await _versions.CreateVersionAsync(lessonId, blobUrl, changelog, cancellationToken));

    [HttpPost("{versionId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(Guid versionId, CancellationToken cancellationToken)
    {
        await _versions.PublishVersionAsync(versionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("assessment-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LinkAssessment([FromBody] LinkAssessmentVersionRequest request, CancellationToken cancellationToken)
    {
        await _versions.LinkAssessmentVersionAsync(request, cancellationToken);
        return NoContent();
    }
}
