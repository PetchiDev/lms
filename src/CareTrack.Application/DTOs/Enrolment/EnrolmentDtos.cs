namespace CareTrack.Application.DTOs.Enrolment;

public record CreateStudentRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid CohortId);

public record StudentEnrolmentResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    string CohortName,
    DateTime CreatedAt,
    DateTime? ActivatedAt);

public record CreateCohortRequest(
    Guid UniversityId,
    Guid ProgrammeId,
    string Name,
    int IntakeYear,
    int CurrentYear,
    int CurrentSemester);

public record CohortResponse(
    Guid Id,
    string Name,
    int IntakeYear,
    int CurrentYear,
    int CurrentSemester,
    string ProgrammeName);

public record CsvImportResult(int TotalRows, int SuccessCount, int FailedCount, IReadOnlyList<string> Errors);
