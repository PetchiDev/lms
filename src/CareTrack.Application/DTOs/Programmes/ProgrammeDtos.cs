namespace CareTrack.Application.DTOs.Programmes;

public record CreateProgrammeRequest(string Name, string Code, string Description, int DurationYears);

public record ProgrammeResponse(Guid Id, string Name, string Code, string Description, int DurationYears);

public record CreateProgrammeYearRequest(int YearNumber, string Name);

public record CreateSemesterRequest(int SemesterNumber, string Name);

public record CreateModuleRequest(string Title, string Description, int SortOrder);

public record ProgrammeStructureResponse(
    Guid Id,
    string Name,
    IReadOnlyList<ProgrammeYearResponse> Years);

public record ProgrammeYearResponse(
    Guid Id,
    int YearNumber,
    string Name,
    IReadOnlyList<SemesterResponse> Semesters);

public record SemesterResponse(
    Guid Id,
    int SemesterNumber,
    string Name,
    IReadOnlyList<ModuleSummaryResponse> Modules);

public record ModuleSummaryResponse(Guid Id, string Title, string Description, int SortOrder);

public record ProgrammeCatalogueResponse(
    IReadOnlyList<ProgrammeCatalogueItemResponse> Programmes,
    int TotalProgrammes,
    int TotalSemesters,
    int TotalModules,
    int ModulesWithContent,
    int ModulesWithAssessment,
    int UniversityMappings);

public record ProgrammeCatalogueItemResponse(
    Guid Id,
    string Name,
    string Code,
    int DurationYears,
    IReadOnlyList<CatalogueUniversityResponse> Universities,
    IReadOnlyList<YearCatalogueResponse> Years);

public record CatalogueUniversityResponse(Guid Id, string Name, string Domain);

public record YearCatalogueResponse(
    int YearNumber,
    string Name,
    IReadOnlyList<SemesterCatalogueResponse> Semesters);

public record SemesterCatalogueResponse(
    Guid Id,
    int SemesterNumber,
    string Name,
    int ModuleCount,
    int ModulesWithContent,
    int ModulesWithAssessment,
    IReadOnlyList<ModuleCatalogueResponse> Modules);

public record ModuleCatalogueResponse(
    Guid Id,
    string Title,
    int LessonCount,
    int PublishedLessonCount,
    int LessonsWithAssets,
    bool HasAssessment,
    int AssessmentQuestionCount,
    bool IsContentReady,
    bool IsAssessmentReady);
