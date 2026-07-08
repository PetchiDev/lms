using CareTrack.Application.DTOs.Certificates;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace CareTrack.Infrastructure.Services;

public class MarksheetService : IMarksheetService
{
    private readonly CareTrackDbContext _db;
    private readonly CareTrack.Application.Common.ITenantContext _tenant;
    private readonly IBlobStorageService _blobStorage;

    public MarksheetService(CareTrackDbContext db, CareTrack.Application.Common.ITenantContext tenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _tenant = tenant;
        _blobStorage = blobStorage;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(byte[] PdfBytes, string FileName)> RenderForCurrentStudentAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != CareTrack.Domain.Enums.UserRole.Student || !_tenant.StudentId.HasValue)
            throw new ForbiddenException("Student access only.");

        var studentId = _tenant.StudentId.Value;

        var attempt = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.QuizId == quizId && a.StudentId == studentId && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new { a.ScorePercent, a.SubmittedAt })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No submitted attempt found for this assessment.");

        var quiz = await _db.Quizzes.AsNoTracking()
            .Where(q => q.Id == quizId)
            .Select(q => new { q.Title, q.PassPercentage, q.ModuleId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Quiz not found.");

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        var enrolment = await _db.StudentEnrolments.AsNoTracking()
            .Include(e => e.Cohort).ThenInclude(c => c.Programme)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.UniversityId, ProgrammeName = e.Cohort.Programme.Name, ProgrammeId = e.Cohort.ProgrammeId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student enrolment not found.");

        var template = await GetOrCreateTemplateAsync(enrolment.UniversityId, cancellationToken);
        var university = await _db.Universities.AsNoTracking()
            .Where(u => u.Id == enrolment.UniversityId)
            .Select(u => new { u.Name, u.LogoUrl })
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(template.OrganizationName) && university is not null)
            template.OrganizationName = university.Name;
        if (string.IsNullOrWhiteSpace(template.LogoUrl) && university is not null)
            template.LogoUrl = university.LogoUrl;

        var certNumber = await _db.Certificates.AsNoTracking()
            .Where(c => c.StudentId == studentId && c.ProgrammeId == enrolment.ProgrammeId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => c.CertificateNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var studentName = $"{student.FirstName} {student.LastName}".Trim();
        var ctx = new MarksheetRenderContext(
            studentName,
            enrolment.ProgrammeName,
            quiz.Title,
            attempt.ScorePercent,
            quiz.PassPercentage,
            certNumber,
            attempt.SubmittedAt!.Value,
            MapTemplate(template));

        var pdfBytes = MarksheetPdfRenderer.Render(ctx, LoadImageBytes);
        var safeTitle = MakeSafeFileName(quiz.Title);
        var fileName = $"Marksheet-{safeTitle}-{attempt.SubmittedAt!.Value:yyyyMMdd}.pdf";
        return (pdfBytes, fileName);
    }

    private async Task<CareTrack.Domain.Entities.CertificateTemplate> GetOrCreateTemplateAsync(Guid? universityId, CancellationToken cancellationToken)
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

        template = new CareTrack.Domain.Entities.CertificateTemplate
        {
            UniversityId = universityId,
            OrganizationName = defaults?.Name ?? "Institute",
            LogoUrl = defaults?.LogoUrl
        };

        _db.CertificateTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return template;
    }

    private static CertificateTemplateResponse MapTemplate(CareTrack.Domain.Entities.CertificateTemplate t) =>
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

        // Blob storage is the source of truth for uploaded assets.
        var bytes = _blobStorage.DownloadAsync(url).GetAwaiter().GetResult();
        return bytes;
    }

    private static string MakeSafeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Assessment";
        foreach (var c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '-');
        return input.Trim().Replace(' ', '-');
    }
}

