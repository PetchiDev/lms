using CareTrack.Application.DTOs.Reports;

namespace CareTrack.Application.Interfaces;

public interface IReportingService
{
    Task<CohortReportResponse> GetUniversityStudentReportAsync(Guid? cohortId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UniversityComparisonReport>> GetApolloUniversityReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentPerformanceReport>> GetContentPerformanceAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportAsync(ExportReportRequest request, CancellationToken cancellationToken = default);
}
