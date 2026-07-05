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
