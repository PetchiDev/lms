namespace CareTrack.Application.DTOs.Calendar;

public record CalendarEventResponse(
    Guid Id, string Title, string Description, DateTime StartAt, DateTime EndAt,
    string EventType, string? JoinUrl, bool CanJoin);
public record CreateCalendarEventRequest(string Title, string Description, DateTime StartAt, DateTime EndAt, string EventType, Guid? CohortId, string? JoinUrl);
public record DiscussionThreadResponse(Guid Id, string Title, int PostCount, DateTime CreatedAt);
public record DiscussionPostResponse(Guid Id, string Body, string AuthorName, DateTime CreatedAt, IReadOnlyList<DiscussionPostResponse> Replies);
public record CreateThreadRequest(string Title, string Body);
public record CreatePostRequest(string Body, Guid? ParentPostId);
public record ContentVersionResponse(Guid Id, int VersionNumber, string Status, DateTime? PublishedAt, string? Changelog);
public record LinkAssessmentVersionRequest(Guid QuizId, Guid ContentVersionId, string? Notes);
