namespace CareTrack.Application.DTOs.Enrolment;

public record CreateStudentRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid CohortId,
    string Password);

public record StudentEnrolmentResponse(
    Guid Id,
    Guid StudentId,
    Guid CohortId,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    string CohortName,
    string ProgrammeName,
    DateTime CreatedAt,
    DateTime? ActivatedAt);

public record AssignStudentCohortRequest(Guid CohortId);

public record UpdateStudentRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Password,
    Guid? CohortId,
    string? Status);

public record CreateCohortRequest(
    Guid UniversityId,
    Guid ProgrammeId,
    string Name,
    int IntakeYear,
    int CurrentYear,
    int CurrentSemester);

public record CohortResponse(
    Guid Id,
    Guid UniversityId,
    Guid ProgrammeId,
    string Name,
    int IntakeYear,
    int CurrentYear,
    int CurrentSemester,
    string ProgrammeName);

public record CsvImportResult(int TotalRows, int SuccessCount, int FailedCount, IReadOnlyList<string> Errors);
