using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Content;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class ContentService : IContentService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBlobStorageService _blobStorage;

    public ContentService(CareTrackDbContext db, ITenantContext tenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _tenant = tenant;
        _blobStorage = blobStorage;
    }

    public async Task<IReadOnlyList<ModulePickerResponse>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can browse content modules.");

        return await _db.Modules.AsNoTracking()
            .OrderBy(m => m.Semester.ProgrammeYear.Programme.Name)
            .ThenBy(m => m.Semester.ProgrammeYear.YearNumber)
            .ThenBy(m => m.Semester.SemesterNumber)
            .ThenBy(m => m.SortOrder)
            .Select(m => new ModulePickerResponse(
                m.Id,
                m.Title,
                m.Semester.Name,
                m.Semester.ProgrammeYear.Name,
                m.Semester.ProgrammeYear.Programme.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can create lessons.");

        _ = await _db.Modules.FindAsync([request.ModuleId], cancellationToken)
            ?? throw new NotFoundException("Module not found.");

        var lesson = new Lesson
        {
            ModuleId = request.ModuleId,
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder,
            CreatedByUserId = _tenant.UserId,
            Status = ContentStatus.Draft
        };

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(cancellationToken);

        return await MapLessonAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonResponse> GetLessonAsync(Guid id, CancellationToken cancellationToken = default)
        => await MapLessonAsync(id, cancellationToken);

    public async Task<LessonResponse> UpdateLessonAsync(Guid id, UpdateLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can update lessons.");

        var lesson = await _db.Lessons.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        if (lesson.Status == ContentStatus.Published)
            throw new ConflictException("Published lessons cannot be edited.");

        lesson.Title = request.Title;
        lesson.Description = request.Description;
        lesson.SortOrder = request.SortOrder;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapLessonAsync(id, cancellationToken);
    }

    public async Task<LessonAssetResponse> UploadAssetAsync(Guid lessonId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can upload assets.");

        var lesson = await _db.Lessons.FindAsync([lessonId], cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        var blobUrl = await _blobStorage.UploadAsync(fileStream, fileName, contentType, cancellationToken);
        var assetType = contentType.StartsWith("video/") ? AssetType.Video
            : contentType == "application/pdf" ? AssetType.Pdf : AssetType.Document;

        var asset = new LessonAsset
        {
            LessonId = lessonId,
            AssetType = assetType,
            FileName = fileName,
            BlobUrl = blobUrl,
            ContentType = contentType,
            FileSizeBytes = fileStream.CanSeek ? fileStream.Length : 0
        };

        _db.LessonAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);

        return new LessonAssetResponse(asset.Id, asset.AssetType.ToString(), asset.FileName, asset.BlobUrl, asset.ContentType, asset.FileSizeBytes);
    }

    public async Task<LessonResponse> UpdateStatusAsync(Guid id, UpdateLessonStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can update lesson status.");

        if (!Enum.TryParse<ContentStatus>(request.Status, out var status))
            throw new ValidationException("Invalid status.");

        var lesson = await _db.Lessons.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        lesson.Status = status;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await MapLessonAsync(id, cancellationToken);
    }

    public async Task PublishAsync(Guid id, PublishLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin && _tenant.Role != UserRole.ApolloFaculty)
            throw new ForbiddenException("Only Apollo users can publish content.");

        var lesson = await _db.Lessons.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        if (lesson.Status != ContentStatus.PendingReview && lesson.Status != ContentStatus.Published)
            throw new ConflictException("Lesson must be approved before publishing.");

        var existing = await _db.ContentPublications.Where(p => p.LessonId == id).ToListAsync(cancellationToken);
        _db.ContentPublications.RemoveRange(existing);

        if (request.UniversityIds is null || request.UniversityIds.Count == 0)
        {
            _db.ContentPublications.Add(new ContentPublication
            {
                LessonId = id,
                UniversityId = null,
                PublishedByUserId = _tenant.UserId
            });
        }
        else
        {
            foreach (var universityId in request.UniversityIds)
            {
                _db.ContentPublications.Add(new ContentPublication
                {
                    LessonId = id,
                    UniversityId = universityId,
                    PublishedByUserId = _tenant.UserId
                });
            }
        }

        lesson.Status = ContentStatus.Published;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can approve content.");

        var lesson = await _db.Lessons.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        if (lesson.Status != ContentStatus.PendingReview)
            throw new ConflictException("Lesson is not pending review.");

        lesson.Status = ContentStatus.PendingReview; // Ready for publish step
        lesson.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<LessonResponse> MapLessonAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Lessons.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LessonResponse(
                l.Id,
                l.ModuleId,
                l.Title,
                l.Description,
                l.Status.ToString(),
                l.SortOrder,
                l.Assets.Select(a => new LessonAssetResponse(
                    a.Id, a.AssetType.ToString(), a.FileName, a.BlobUrl, a.ContentType, a.FileSizeBytes)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");
    }
}
