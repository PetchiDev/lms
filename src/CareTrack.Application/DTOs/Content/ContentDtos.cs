namespace CareTrack.Application.DTOs.Content;

public record CreateLessonRequest(Guid ModuleId, string Title, string Description, int SortOrder);

public record UpdateLessonRequest(string Title, string Description, int SortOrder);

public record LessonResponse(
    Guid Id,
    Guid ModuleId,
    string Title,
    string Description,
    string Status,
    int SortOrder,
    IReadOnlyList<LessonAssetResponse> Assets,
    IReadOnlyList<PublishedUniversityInfo> PublishedTo);

public record PublishedUniversityInfo(Guid? UniversityId, string Name);

public record LessonListItemResponse(
    Guid Id,
    string Title,
    string Status,
    int AssetCount,
    IReadOnlyList<PublishedUniversityInfo> PublishedTo);

public record LessonAssetResponse(
    Guid Id,
    string AssetType,
    string FileName,
    string BlobUrl,
    string ContentType,
    long FileSizeBytes);

public record UpdateLessonStatusRequest(string Status);

public record PublishLessonRequest(IReadOnlyList<Guid>? UniversityIds);

public record PublishModuleRequest(IReadOnlyList<Guid>? UniversityIds);

public record MapProgrammesToUniversitiesRequest(
    IReadOnlyList<Guid> ProgrammeIds,
    IReadOnlyList<Guid> UniversityIds);

public record MapProgrammesToUniversitiesResponse(
    int ProgrammeLinksAdded,
    int ModulesIncluded,
    int LessonsMapped);

public record ModulePickerResponse(
    Guid ModuleId,
    string ModuleTitle,
    string SemesterName,
    string YearName,
    string ProgrammeName);
