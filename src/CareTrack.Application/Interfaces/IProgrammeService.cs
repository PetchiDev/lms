using CareTrack.Application.DTOs.Programmes;

namespace CareTrack.Application.Interfaces;

public interface IProgrammeService
{
    Task<ProgrammeResponse> CreateAsync(CreateProgrammeRequest request, CancellationToken cancellationToken = default);
    Task<ProgrammeStructureResponse> GetStructureAsync(Guid programmeId, CancellationToken cancellationToken = default);
    Task<ProgrammeYearResponse> AddYearAsync(Guid programmeId, CreateProgrammeYearRequest request, CancellationToken cancellationToken = default);
    Task<SemesterResponse> AddSemesterAsync(Guid yearId, CreateSemesterRequest request, CancellationToken cancellationToken = default);
    Task<ModuleSummaryResponse> AddModuleAsync(Guid semesterId, CreateModuleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProgrammeResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProgrammeCatalogueResponse> GetCatalogueAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid programmeId, CancellationToken cancellationToken = default);
    Task DeleteModuleAsync(Guid moduleId, CancellationToken cancellationToken = default);
}
