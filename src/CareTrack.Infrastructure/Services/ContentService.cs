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

    public async Task<IReadOnlyList<LessonListItemResponse>> GetModuleLessonsAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can browse content.");

        if (!await _db.Modules.AsNoTracking().AnyAsync(m => m.Id == moduleId, cancellationToken))
            throw new NotFoundException("Module not found.");

        var lessons = await _db.Lessons.AsNoTracking()
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Title)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Status,
                AssetCount = l.Assets.Count,
                Publications = l.Publications.Select(p => p.UniversityId).ToList()
            })
            .ToListAsync(cancellationToken);

        var universityNames = await _db.Universities.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return lessons.Select(l => new LessonListItemResponse(
            l.Id,
            l.Title,
            l.Status.ToString(),
            l.AssetCount,
            MapPublishedUniversities(l.Publications, universityNames))).ToList();
    }

    public async Task<int> PublishModuleAsync(Guid moduleId, PublishModuleRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin && _tenant.Role != UserRole.ApolloFaculty)
            throw new ForbiddenException("Only Apollo users can publish content.");

        var lessonIds = await _db.Lessons.AsNoTracking()
            .Where(l => l.ModuleId == moduleId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        if (lessonIds.Count == 0)
            throw new NotFoundException("No lessons found in this module.");

        foreach (var lessonId in lessonIds)
            await PublishAsync(lessonId, new PublishLessonRequest(request.UniversityIds), cancellationToken);

        return lessonIds.Count;
    }

    public async Task<MapProgrammesToUniversitiesResponse> MapProgrammesToUniversitiesAsync(
        MapProgrammesToUniversitiesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin && _tenant.Role != UserRole.ApolloFaculty)
            throw new ForbiddenException("Only Apollo users can map programme content.");

        var programmeIds = request.ProgrammeIds?.Distinct().ToList() ?? [];
        var universityIds = request.UniversityIds?.Distinct().ToList() ?? [];

        if (programmeIds.Count == 0 || universityIds.Count == 0)
            throw new ValidationException("Select at least one programme and one university.");

        var programmeCount = await _db.Programmes.AsNoTracking()
            .CountAsync(p => programmeIds.Contains(p.Id), cancellationToken);
        if (programmeCount != programmeIds.Count)
            throw new NotFoundException("One or more programmes were not found.");

        var universityCount = await _db.Universities.AsNoTracking()
            .CountAsync(u => universityIds.Contains(u.Id), cancellationToken);
        if (universityCount != universityIds.Count)
            throw new NotFoundException("One or more universities were not found.");

        var programmeLinksAdded = 0;
        var modulesIncluded = 0;
        var lessonsMapped = 0;

        foreach (var universityId in universityIds)
        {
            foreach (var programmeId in programmeIds)
            {
                var linked = await _db.UniversityProgrammes.AnyAsync(
                    up => up.UniversityId == universityId && up.ProgrammeId == programmeId,
                    cancellationToken);

                if (!linked)
                {
                    _db.UniversityProgrammes.Add(new UniversityProgramme
                    {
                        UniversityId = universityId,
                        ProgrammeId = programmeId,
                    });
                    programmeLinksAdded++;
                    await _db.SaveChangesAsync(cancellationToken);
                }

                await EnsureDefaultCohortAsync(universityId, programmeId, cancellationToken);

                var moduleIds = await _db.Modules.AsNoTracking()
                    .Where(m => m.Semester.ProgrammeYear.ProgrammeId == programmeId)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                modulesIncluded += moduleIds.Count;

                foreach (var moduleId in moduleIds)
                {
                    var lessonIds = await _db.Lessons.AsNoTracking()
                        .Where(l => l.ModuleId == moduleId)
                        .Select(l => l.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var lessonId in lessonIds)
                    {
                        var before = await _db.ContentPublications.AsNoTracking()
                            .CountAsync(
                                p => p.LessonId == lessonId && p.UniversityId == universityId,
                                cancellationToken);

                        await PublishAsync(
                            lessonId,
                            new PublishLessonRequest([universityId]),
                            cancellationToken);

                        var after = await _db.ContentPublications.AsNoTracking()
                            .CountAsync(
                                p => p.LessonId == lessonId && p.UniversityId == universityId,
                                cancellationToken);

                        if (after > before)
                            lessonsMapped++;
                    }
                }
            }
        }

        return new MapProgrammesToUniversitiesResponse(programmeLinksAdded, modulesIncluded, lessonsMapped);
    }

    public async Task PublishProgrammeLessonsToUniversityAsync(
        Guid programmeId,
        Guid universityId,
        CancellationToken cancellationToken = default)
    {
        var lessonIds = await _db.Lessons.AsNoTracking()
            .Where(l => l.Status == ContentStatus.Published)
            .Where(l => l.Module.Semester.ProgrammeYear.ProgrammeId == programmeId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        if (lessonIds.Count == 0)
            return;

        var alreadyPublished = await _db.ContentPublications
            .Where(p => p.UniversityId == universityId && lessonIds.Contains(p.LessonId))
            .Select(p => p.LessonId)
            .ToHashSetAsync(cancellationToken);

        foreach (var lessonId in lessonIds)
        {
            if (alreadyPublished.Contains(lessonId))
                continue;

            _db.ContentPublications.Add(new ContentPublication
            {
                LessonId = lessonId,
                UniversityId = universityId,
                PublishedByUserId = _tenant.UserId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
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

        if (lesson.Status == ContentStatus.Published && _tenant.Role != UserRole.ApolloAdmin)
            throw new ConflictException("Published lessons can only be edited by Apollo Admin.");

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

        if (lesson.Status == ContentStatus.Published && _tenant.Role != UserRole.ApolloAdmin)
            throw new ConflictException("Published lesson assets can only be updated by Apollo Admin.");

        var blobUrl = await _blobStorage.UploadAsync(fileStream, fileName, contentType, "media/lessons", cancellationToken);
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

    public async Task DeleteAssetAsync(Guid lessonId, Guid assetId, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can delete assets.");

        var lesson = await _db.Lessons.AsNoTracking()
            .Where(l => l.Id == lessonId)
            .Select(l => new { l.Id, l.Status })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        if (lesson.Status == ContentStatus.Published && _tenant.Role != UserRole.ApolloAdmin)
            throw new ConflictException("Published lesson assets can only be updated by Apollo Admin.");

        var asset = await _db.LessonAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.LessonId == lessonId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        await _blobStorage.DeleteAsync(asset.BlobUrl, cancellationToken);
        _db.LessonAssets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
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

        var canPublish = lesson.Status == ContentStatus.PendingReview
            || lesson.Status == ContentStatus.Published
            || (_tenant.Role == UserRole.ApolloAdmin && lesson.Status == ContentStatus.Draft);

        if (!canPublish)
            throw new ConflictException("Lesson must be approved before publishing.");

        var programmeId = await _db.Lessons.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => l.Module.Semester.ProgrammeYear.ProgrammeId)
            .FirstOrDefaultAsync(cancellationToken);

        var existing = await _db.ContentPublications.Where(p => p.LessonId == id).ToListAsync(cancellationToken);

        if (request.UniversityIds is null || request.UniversityIds.Count == 0)
        {
            if (existing.All(p => p.UniversityId.HasValue))
            {
                _db.ContentPublications.Add(new ContentPublication
                {
                    LessonId = id,
                    UniversityId = null,
                    PublishedByUserId = _tenant.UserId
                });
            }

            lesson.Status = ContentStatus.Published;
            lesson.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var existingUniversityIds = existing
                .Where(p => p.UniversityId.HasValue)
                .Select(p => p.UniversityId!.Value)
                .ToHashSet();

            var universitiesNeedingCohort = new List<Guid>();

            foreach (var universityId in request.UniversityIds.Distinct())
            {
                if (existingUniversityIds.Contains(universityId))
                    continue;

                _db.ContentPublications.Add(new ContentPublication
                {
                    LessonId = id,
                    UniversityId = universityId,
                    PublishedByUserId = _tenant.UserId
                });

                if (programmeId != Guid.Empty)
                {
                    var linked = await _db.UniversityProgrammes.AnyAsync(
                        up => up.UniversityId == universityId && up.ProgrammeId == programmeId,
                        cancellationToken);
                    if (!linked)
                    {
                        _db.UniversityProgrammes.Add(new UniversityProgramme
                        {
                            UniversityId = universityId,
                            ProgrammeId = programmeId
                        });
                    }

                    universitiesNeedingCohort.Add(universityId);
                }
            }

            lesson.Status = ContentStatus.Published;
            lesson.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var universityId in universitiesNeedingCohort.Distinct())
                await EnsureDefaultCohortAsync(universityId, programmeId, cancellationToken);
        }
    }

    private async Task EnsureDefaultCohortAsync(Guid universityId, Guid programmeId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;

        var hasCohort = await _db.Cohorts.IgnoreQueryFilters()
            .AnyAsync(c => c.UniversityId == universityId && c.ProgrammeId == programmeId && c.IntakeYear == year, cancellationToken);

        if (hasCohort) return;

        var programmeCode = await _db.Programmes.AsNoTracking()
            .Where(p => p.Id == programmeId)
            .Select(p => p.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var label = string.IsNullOrWhiteSpace(programmeCode) ? "Programme" : programmeCode.Trim();

        _db.Cohorts.Add(new Cohort
        {
            UniversityId = universityId,
            ProgrammeId = programmeId,
            Name = $"{label} {year} Intake",
            IntakeYear = year,
            CurrentYear = 1,
            CurrentSemester = 1
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can approve content.");

        var lesson = await _db.Lessons.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        if (lesson.Status == ContentStatus.Published)
            throw new ConflictException("Lesson is already published.");

        if (lesson.Status != ContentStatus.Draft && lesson.Status != ContentStatus.PendingReview)
            throw new ConflictException("Lesson cannot be approved in its current state.");

        lesson.Status = ContentStatus.PendingReview;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<LessonResponse> MapLessonAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await _db.Lessons.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id,
                l.ModuleId,
                l.Title,
                l.Description,
                l.Status,
                l.SortOrder,
                Assets = l.Assets.Select(a => new LessonAssetResponse(
                    a.Id, a.AssetType.ToString(), a.FileName, a.BlobUrl, a.ContentType, a.FileSizeBytes)).ToList(),
                Publications = l.Publications.Select(p => p.UniversityId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lesson not found.");

        var universityNames = await _db.Universities.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return new LessonResponse(
            lesson.Id,
            lesson.ModuleId,
            lesson.Title,
            lesson.Description,
            lesson.Status.ToString(),
            lesson.SortOrder,
            lesson.Assets,
            MapPublishedUniversities(lesson.Publications, universityNames));
    }

    private static IReadOnlyList<PublishedUniversityInfo> MapPublishedUniversities(
        IReadOnlyList<Guid?> publicationUniversityIds,
        IReadOnlyDictionary<Guid, string> universityNames)
    {
        if (publicationUniversityIds.Any(id => id is null))
            return [new PublishedUniversityInfo(null, "All universities")];

        return publicationUniversityIds
            .Where(id => id.HasValue)
            .Select(id => new PublishedUniversityInfo(id, universityNames.GetValueOrDefault(id!.Value, "Unknown")))
            .DistinctBy(p => p.UniversityId)
            .ToList();
    }
}
