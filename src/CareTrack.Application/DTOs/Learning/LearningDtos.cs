namespace CareTrack.Application.DTOs.Learning;

public record StudentDashboardResponse(
    string StudentName,
    string CohortName,
    int CurrentYear,
    int CurrentSemester,
    int OverallProgressPercent,
    IReadOnlyList<EnrolledModuleResponse> Modules,
    IReadOnlyList<string> Notices);

public record EnrolledModuleResponse(
    Guid Id,
    string Title,
    int ProgressPercent,
    bool IsLocked,
    string? LockReason,
    bool IsCompleted);

public record ModuleDetailResponse(
    Guid Id,
    string Title,
    string Description,
    int ProgressPercent,
    bool IsLocked,
    string? LockReason,
    IReadOnlyList<LessonSummaryResponse> Lessons);

public record LessonSummaryResponse(
    Guid Id,
    string Title,
    int ProgressPercent,
    string Status,
    bool IsCompleted);

public record LessonDetailResponse(
    Guid Id,
    string Title,
    string Description,
    int ProgressPercent,
    string Status,
    IReadOnlyList<LessonAssetResponse> Assets);

public record LessonAssetResponse(
    Guid Id,
    string AssetType,
    string FileName,
    string BlobUrl,
    string ContentType);

public record UpdateLessonProgressRequest(int WatchedSeconds, int ProgressPercent);

public record MarkLessonCompleteResponse(bool Success, string? Message);

public record BulkCompleteResponse(
    int ModuleProgressPercent,
    bool ModuleCompleted,
    bool AllCurriculumComplete,
    int OverallProgressPercent);
