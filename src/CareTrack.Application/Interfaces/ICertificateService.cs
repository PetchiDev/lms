using CareTrack.Application.DTOs.Certificates;

namespace CareTrack.Application.Interfaces;

public interface ICertificateService
{
    Task<CertificateTemplateResponse> GetTemplateAsync(CancellationToken cancellationToken = default);
    Task<CertificateTemplateResponse> UpdateTemplateAsync(UpdateCertificateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CertificateResponse>> GetMyCertificatesAsync(CancellationToken cancellationToken = default);
    Task<CertificateResponse?> GenerateForCurrentStudentAsync(CancellationToken cancellationToken = default);
}
