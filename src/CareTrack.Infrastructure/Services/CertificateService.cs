using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Certificates;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace CareTrack.Infrastructure.Services;

public class CertificateService : ICertificateService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBlobStorageService _blobStorage;
    private readonly string _uploadsPath;
    private readonly string _defaultLogoPath;

    public CertificateService(CareTrackDbContext db, ITenantContext tenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _tenant = tenant;
        _blobStorage = blobStorage;
        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _defaultLogoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "apollo_logo.png");
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<CertificateTemplateResponse> GetTemplateAsync(CancellationToken cancellationToken = default)
    {
        var template = await GetOrCreateTemplateAsync(GetTemplateUniversityId(), cancellationToken);
        return MapTemplate(template);
    }

    public async Task<CertificateTemplateResponse> UpdateTemplateAsync(UpdateCertificateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role is not (UserRole.ApolloAdmin or UserRole.UniversityAdmin))
            throw new ForbiddenException("Only administrators can update the certificate template.");

        var templateUniversityId = GetTemplateUniversityId(requireUniversity: _tenant.Role == UserRole.UniversityAdmin);
        var template = await GetOrCreateTemplateAsync(templateUniversityId, cancellationToken);
        template.Title = request.Title.Trim();
        template.OrganizationName = request.OrganizationName.Trim();
        template.Tagline = request.Tagline.Trim();
        template.AwardedToLabel = request.AwardedToLabel.Trim();
        template.BodyText = request.BodyText.Trim();
        template.DatePrefix = request.DatePrefix.Trim();
        template.Location = request.Location.Trim();
        template.FooterLocation = request.FooterLocation.Trim();
        template.WebsiteUrl = request.WebsiteUrl.Trim();
        template.LeftSignatoryTitle = request.LeftSignatoryTitle.Trim();
        template.RightSignatoryTitle = request.RightSignatoryTitle.Trim();
        template.LogoUrl = request.LogoUrl;
        template.LeftSignatureImageUrl = request.LeftSignatureImageUrl;
        template.RightSignatureImageUrl = request.RightSignatureImageUrl;
        template.PrimaryColor = request.PrimaryColor.Trim();
        template.AccentColor = request.AccentColor.Trim();
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<IReadOnlyList<CertificateResponse>> GetMyCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        // Keep API stable even if legacy duplicate rows exist.
        // EF translation for "group then project navigation property" can fail, so we dedupe in-memory.
        var raw = await _db.Certificates.AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new
            {
                c.Id,
                c.CertificateNumber,
                ProgrammeId = c.ProgrammeId,
                ProgrammeName = c.Programme.Name,
                c.IssuedAt,
                c.PdfBlobUrl
            })
            .ToListAsync(cancellationToken);

        return raw
            .GroupBy(x => x.ProgrammeId)
            .Select(g => g.First()) // already ordered by issuedAt desc
            .Select(c => new CertificateResponse(c.Id, c.CertificateNumber, c.ProgrammeName, c.IssuedAt, c.PdfBlobUrl))
            .OrderByDescending(x => x.IssuedAt)
            .ToList();
    }

    public async Task<CertificateResponse?> GenerateForCurrentStudentAsync(CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        var student = await _db.Students.AsNoTracking()
            .Include(s => s.Enrolments).ThenInclude(e => e.Cohort).ThenInclude(c => c.Programme)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        var enrolment = student.Enrolments.First();
        var programme = enrolment.Cohort.Programme;
        var universityId = enrolment.UniversityId;

        var existing = await _db.Certificates.AsNoTracking()
            .Include(c => c.Programme)
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.ProgrammeId == programme.Id, cancellationToken);

        if (existing is not null)
            return new CertificateResponse(existing.Id, existing.CertificateNumber, existing.Programme.Name, existing.IssuedAt, existing.PdfBlobUrl);

        var template = await GetOrCreateTemplateAsync(universityId, cancellationToken);
        var university = await _db.Universities.AsNoTracking()
            .Where(u => u.Id == universityId)
            .Select(u => new { u.Name, u.LogoUrl })
            .FirstOrDefaultAsync(cancellationToken);

        // Ensure Apollo branding never leaks into tenant certificates.
        if (string.IsNullOrWhiteSpace(template.OrganizationName) && university is not null)
            template.OrganizationName = university.Name;
        if (string.IsNullOrWhiteSpace(template.LogoUrl) && university is not null)
            template.LogoUrl = university.LogoUrl;
        var certNumber = $"CT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var issuedAt = DateTime.UtcNow;
        var studentName = $"{student.FirstName} {student.LastName}".Trim();

        var renderContext = new CertificateRenderContext(
            studentName,
            programme.Name,
            certNumber,
            issuedAt,
            MapTemplate(template));

        var pdfBytes = CertificatePdfRenderer.Render(renderContext, LoadImageBytes);
        using var pdfStream = new MemoryStream(pdfBytes);
        var blobUrl = await _blobStorage.UploadAsync(pdfStream, $"{certNumber}.pdf", "application/pdf", $"media/certificates/{universityId}", cancellationToken);

        var certificate = new Certificate
        {
            StudentId = studentId,
            ProgrammeId = programme.Id,
            CertificateNumber = certNumber,
            IssuedAt = issuedAt,
            PdfBlobUrl = blobUrl
        };

        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync(cancellationToken);

        return new CertificateResponse(certificate.Id, certificate.CertificateNumber, programme.Name, certificate.IssuedAt, certificate.PdfBlobUrl);
    }

    private Guid? GetTemplateUniversityId(bool requireUniversity = false)
    {
        if (_tenant.Role is UserRole.ApolloAdmin or UserRole.ApolloFaculty)
            return null;

        if (requireUniversity && !_tenant.UniversityId.HasValue)
            throw new ForbiddenException("University context required.");

        return _tenant.UniversityId;
    }

    private async Task<CertificateTemplate> GetOrCreateTemplateAsync(Guid? universityId, CancellationToken cancellationToken)
    {
        var template = await _db.CertificateTemplates
            .FirstOrDefaultAsync(t => t.UniversityId == universityId, cancellationToken);
        if (template is not null) return template;

        var defaults = universityId.HasValue
            ? await _db.Universities.AsNoTracking()
                .Where(u => u.Id == universityId.Value)
                .Select(u => new { u.Name, u.LogoUrl })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        template = new CertificateTemplate
        {
            UniversityId = universityId,
            OrganizationName = defaults?.Name ?? "Institute",
            LogoUrl = defaults?.LogoUrl
        };

        _db.CertificateTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return template;
    }

    private static CertificateTemplateResponse MapTemplate(CertificateTemplate t) =>
        new(
            t.Id,
            t.Title,
            t.OrganizationName,
            t.Tagline,
            t.AwardedToLabel,
            t.BodyText,
            t.DatePrefix,
            t.Location,
            t.FooterLocation,
            t.WebsiteUrl,
            t.LeftSignatoryTitle,
            t.RightSignatoryTitle,
            t.LogoUrl,
            t.LeftSignatureImageUrl,
            t.RightSignatureImageUrl,
            t.PrimaryColor,
            t.AccentColor);

    private byte[]? LoadImageBytes(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var bytes = _blobStorage.DownloadAsync(url).GetAwaiter().GetResult();
        if (bytes is not null)
            return bytes;

        return null;
    }

    private Guid GetStudentId()
    {
        if (_tenant.Role != UserRole.Student || !_tenant.StudentId.HasValue)
            throw new ForbiddenException("Student access only.");
        return _tenant.StudentId.Value;
    }
}
