namespace CareTrack.Application.DTOs.Clinical;

public record HospitalDepartmentResponse(Guid Id, string Name, string Code, int CapacityPerMonth);
public record RotationResponse(Guid Id, string Name, string DepartmentName, DateOnly StartDate, DateOnly EndDate, string Status, int AssignedStudents);
public record RotationAssignmentResponse(Guid Id, Guid RotationId, string RotationName, string StudentName, decimal AttendancePercent, int CompletedProcedureCount, string Status);
public record LogbookEntryResponse(
    Guid Id, DateOnly EntryDate, string Procedure, int PatientCount, string Notes, string Location,
    string Status, string StudentName, string? SupervisorRemarks, DateTime SubmittedAt, DateTime? ReviewedAt, bool IsEscalated);
public record CreateLogbookEntryRequest(Guid RotationAssignmentId, DateOnly EntryDate, string Procedure, int PatientCount, string Notes, string Location);
public record SignOffRequest(string Action, string? Remarks);
public record CreateRotationRequest(Guid HospitalDepartmentId, Guid CohortId, string Name, DateOnly StartDate, DateOnly EndDate, int RequiredProcedureCount);
public record AssignStudentsRequest(IReadOnlyList<Guid> StudentIds);
public record SupervisorDashboardResponse(int PendingCount, int EscalatedCount, int ApprovedToday, IReadOnlyList<LogbookEntryResponse> PendingEntries);
