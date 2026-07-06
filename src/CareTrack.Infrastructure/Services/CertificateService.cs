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
        EnsureApolloUser();
        var template = await GetOrCreateTemplateAsync(cancellationToken);
        return MapTemplate(template);
    }

    public async Task<CertificateTemplateResponse> UpdateTemplateAsync(UpdateCertificateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo administrators can update the certificate template.");

        var template = await GetOrCreateTemplateAsync(cancellationToken);
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

        return await _db.Certificates.AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new CertificateResponse(
                c.Id,
                c.CertificateNumber,
                c.Programme.Name,
                c.IssuedAt,
                c.PdfBlobUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<CertificateResponse?> GenerateForCurrentStudentAsync(CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        var student = await _db.Students.AsNoTracking()
            .Include(s => s.Enrolments).ThenInclude(e => e.Cohort).ThenInclude(c => c.Programme)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        var programme = student.Enrolments.First().Cohort.Programme;

        var existing = await _db.Certificates.AsNoTracking()
            .Include(c => c.Programme)
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.ProgrammeId == programme.Id, cancellationToken);

        if (existing is not null)
            return new CertificateResponse(existing.Id, existing.CertificateNumber, existing.Programme.Name, existing.IssuedAt, existing.PdfBlobUrl);

        var template = await GetOrCreateTemplateAsync(cancellationToken);
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
        var blobUrl = await _blobStorage.UploadAsync(pdfStream, $"{certNumber}.pdf", "application/pdf", cancellationToken);

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

    private async Task<CertificateTemplate> GetOrCreateTemplateAsync(CancellationToken cancellationToken)
    {
        var template = await _db.CertificateTemplates.FirstOrDefaultAsync(cancellationToken);
        if (template is not null) return template;

        template = new CertificateTemplate();
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
            return File.Exists(_defaultLogoPath) ? File.ReadAllBytes(_defaultLogoPath) : null;

        if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(_uploadsPath, url.Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase));
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
            return File.Exists(uri.LocalPath) ? File.ReadAllBytes(uri.LocalPath) : null;

        return null;
    }

    private void EnsureApolloUser()
    {
        if (_tenant.Role is not (UserRole.ApolloAdmin or UserRole.ApolloFaculty))
            throw new ForbiddenException("Apollo staff access only.");
    }

    private Guid GetStudentId()
    {
        if (_tenant.Role != UserRole.Student || !_tenant.StudentId.HasValue)
            throw new ForbiddenException("Student access only.");
        return _tenant.StudentId.Value;
    }
}
