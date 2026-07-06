using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Enrolment;

namespace CareTrack.Application.Interfaces;

public interface IEnrolmentService
{
    Task<StudentEnrolmentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);
    Task<CsvImportResult> ImportStudentsAsync(Stream csvStream, Guid cohortId, CancellationToken cancellationToken = default);
    Task<PagedResult<StudentEnrolmentResponse>> GetStudentsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<StudentEnrolmentResponse> AssignStudentCohortAsync(Guid studentId, AssignStudentCohortRequest request, CancellationToken cancellationToken = default);
    Task<CohortResponse> CreateCohortAsync(CreateCohortRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CohortResponse>> GetCohortsAsync(CancellationToken cancellationToken = default);
}
