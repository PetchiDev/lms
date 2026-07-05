namespace CareTrack.Application.DTOs.Reports;

public record StudentProgressReport(
    Guid StudentId,
    string FullName,
    string Email,
    string CohortName,
    int ProgressPercent,
    bool IsAtRisk,
    DateTime? LastActivityAt);

public record CohortReportResponse(
    string CohortName,
    int TotalStudents,
    int ActiveStudents,
    int AtRiskCount,
    double AverageProgress,
    IReadOnlyList<StudentProgressReport> Students);

public record UniversityComparisonReport(
    Guid UniversityId,
    string UniversityName,
    int TotalStudents,
    double AverageProgress,
    int AtRiskCount);

public record ContentPerformanceReport(
    Guid ModuleId,
    string ModuleTitle,
    string ProgrammeName,
    int EnrolledStudents,
    int CompletedStudents,
    double CompletionRate);

public record ExportReportRequest(string Format, Guid? CohortId);
