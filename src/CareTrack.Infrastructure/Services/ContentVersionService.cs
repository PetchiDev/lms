using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Calendar;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class ContentVersionService : IContentVersionService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;

    public ContentVersionService(CareTrackDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<ContentVersionResponse>> GetLessonVersionsAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        return await _db.ContentVersions.AsNoTracking()
            .Where(v => v.LessonId == lessonId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ContentVersionResponse(v.Id, v.VersionNumber, v.Status.ToString(), v.PublishedAt, v.Changelog))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentVersionResponse> CreateVersionAsync(Guid lessonId, string blobUrl, string? changelog, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Apollo faculty only.");

        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        var maxVersion = await _db.ContentVersions.Where(v => v.LessonId == lessonId).MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;

        var version = new ContentVersion
        {
            LessonId = lessonId,
            VersionNumber = maxVersion + 1,
            BlobUrl = blobUrl,
            Changelog = changelog,
            Status = ContentVersionStatus.Draft,
            CreatedByUserId = _tenant.UserId
        };
        _db.ContentVersions.Add(version);
        await _db.SaveChangesAsync(cancellationToken);

        return new ContentVersionResponse(version.Id, version.VersionNumber, version.Status.ToString(), version.PublishedAt, version.Changelog);
    }

    public async Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Apollo faculty only.");

        var version = await _db.ContentVersions.Include(v => v.Lesson).FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken)
            ?? throw new NotFoundException("Version not found.");

        var activeVersions = await _db.ContentVersions
            .Where(v => v.LessonId == version.LessonId && v.Status == ContentVersionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var active in activeVersions)
        {
            active.Status = ContentVersionStatus.Archived;
            active.ArchivedAt = DateTime.UtcNow;
        }

        version.Status = ContentVersionStatus.Active;
        version.PublishedAt = DateTime.UtcNow;
        version.Lesson.Status = ContentStatus.Published;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkAssessmentVersionAsync(LinkAssessmentVersionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Apollo faculty only.");

        if (await _db.AssessmentContentVersions.AnyAsync(l => l.QuizId == request.QuizId && l.ContentVersionId == request.ContentVersionId, cancellationToken))
            throw new ConflictException("Link already exists.");

        _db.AssessmentContentVersions.Add(new AssessmentContentVersion
        {
            QuizId = request.QuizId,
            ContentVersionId = request.ContentVersionId,
            Notes = request.Notes
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
