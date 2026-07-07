using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Universities;

namespace CareTrack.Application.Interfaces;

public interface IUniversityService
{
    Task<UniversityResponse> CreateAsync(CreateUniversityRequest request, CancellationToken cancellationToken = default);
    Task<UniversityResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<UniversityResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UniversityResponse> UpdateAsync(Guid id, UpdateUniversityRequest request, CancellationToken cancellationToken = default);
    Task<UniversityResponse> SetProgrammesAsync(Guid id, SetUniversityProgrammesRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task InviteUniversityAdminAsync(CreateUniversityAdminRequest request, CancellationToken cancellationToken = default);
    Task<UniversityAdminResponse> CreateUniversityAdminAsync(CreateUniversityAdminDirectRequest request, CancellationToken cancellationToken = default);
}
