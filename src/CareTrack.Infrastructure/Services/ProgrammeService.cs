using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Programmes;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class ProgrammeService : IProgrammeService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBlobStorageService _blobStorage;

    public ProgrammeService(CareTrackDbContext db, ITenantContext tenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _tenant = tenant;
        _blobStorage = blobStorage;
    }

    public async Task<ProgrammeResponse> CreateAsync(CreateProgrammeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can create programmes.");

        if (await _db.Programmes.AnyAsync(p => p.Code == request.Code, cancellationToken))
            throw new ConflictException("Programme code already exists.");

        var programme = new Programme
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            DurationYears = request.DurationYears
        };

        _db.Programmes.Add(programme);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProgrammeResponse(programme.Id, programme.Name, programme.Code, programme.Description, programme.DurationYears);
    }

    public async Task<IReadOnlyList<ProgrammeResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Programmes.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProgrammeResponse(p.Id, p.Name, p.Code, p.Description, p.DurationYears))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProgrammeStructureResponse> GetStructureAsync(Guid programmeId, CancellationToken cancellationToken = default)
    {
        var programme = await _db.Programmes.AsNoTracking()
            .Where(p => p.Id == programmeId)
            .Select(p => new ProgrammeStructureResponse(
                p.Id,
                p.Name,
                p.Years.OrderBy(y => y.YearNumber).Select(y => new ProgrammeYearResponse(
                    y.Id,
                    y.YearNumber,
                    y.Name,
                    y.Semesters.OrderBy(s => s.SemesterNumber).Select(s => new SemesterResponse(
                        s.Id,
                        s.SemesterNumber,
                        s.Name,
                        s.Modules.OrderBy(m => m.SortOrder).Select(m => new ModuleSummaryResponse(
                            m.Id, m.Title, m.Description, m.SortOrder)).ToList()
                    )).ToList()
                )).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Programme not found.");

        return programme;
    }

    public async Task<ProgrammeYearResponse> AddYearAsync(Guid programmeId, CreateProgrammeYearRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can modify programme structure.");

        _ = await _db.Programmes.FindAsync([programmeId], cancellationToken)
            ?? throw new NotFoundException("Programme not found.");

        var year = new ProgrammeYear
        {
            ProgrammeId = programmeId,
            YearNumber = request.YearNumber,
            Name = request.Name
        };

        _db.ProgrammeYears.Add(year);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProgrammeYearResponse(year.Id, year.YearNumber, year.Name, []);
    }

    public async Task<SemesterResponse> AddSemesterAsync(Guid yearId, CreateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can modify programme structure.");

        _ = await _db.ProgrammeYears.FindAsync([yearId], cancellationToken)
            ?? throw new NotFoundException("Programme year not found.");

        var semester = new Semester
        {
            ProgrammeYearId = yearId,
            SemesterNumber = request.SemesterNumber,
            Name = request.Name
        };

        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync(cancellationToken);

        return new SemesterResponse(semester.Id, semester.SemesterNumber, semester.Name, []);
    }

    public async Task<ModuleSummaryResponse> AddModuleAsync(Guid semesterId, CreateModuleRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can modify programme structure.");

        _ = await _db.Semesters.FindAsync([semesterId], cancellationToken)
            ?? throw new NotFoundException("Semester not found.");

        var module = new Module
        {
            SemesterId = semesterId,
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder
        };

        _db.Modules.Add(module);
        await _db.SaveChangesAsync(cancellationToken);

        return new ModuleSummaryResponse(module.Id, module.Title, module.Description, module.SortOrder);
    }

    public async Task<ProgrammeCatalogueResponse> GetCatalogueAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can view the programme catalogue.");

        var programmes = await _db.Programmes.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Code, p.DurationYears })
            .ToListAsync(cancellationToken);

        var universityMap = await _db.UniversityProgrammes.AsNoTracking()
            .Select(up => new
            {
                up.ProgrammeId,
                up.University.Id,
                up.University.Name,
                up.University.Domain
            })
            .ToListAsync(cancellationToken);

        var moduleRows = await _db.Modules.AsNoTracking()
            .Select(m => new
            {
                ProgrammeId = m.Semester.ProgrammeYear.ProgrammeId,
                YearNumber = m.Semester.ProgrammeYear.YearNumber,
                YearName = m.Semester.ProgrammeYear.Name,
                m.SemesterId,
                SemesterNumber = m.Semester.SemesterNumber,
                SemesterName = m.Semester.Name,
                m.Id,
                m.Title,
                LessonCount = m.Lessons.Count,
                PublishedLessonCount = m.Lessons.Count(l => l.Status == ContentStatus.Published),
                LessonsWithAssets = m.Lessons.Count(l => l.Assets.Any()),
                QuizQuestionCount = m.Quizzes
                    .Where(q => q.IsActive)
                    .SelectMany(q => q.Questions)
                    .Count()
            })
            .ToListAsync(cancellationToken);

        var items = new List<ProgrammeCatalogueItemResponse>();
        var totalSemesters = 0;
        var totalModules = moduleRows.Count;
        var modulesWithContent = 0;
        var modulesWithAssessment = 0;

        foreach (var programme in programmes)
        {
            var universities = universityMap
                .Where(u => u.ProgrammeId == programme.Id)
                .Select(u => new CatalogueUniversityResponse(u.Id, u.Name, u.Domain))
                .DistinctBy(u => u.Id)
                .OrderBy(u => u.Name)
                .ToList();

            var programmeModules = moduleRows.Where(m => m.ProgrammeId == programme.Id).ToList();

            var years = programmeModules
                .GroupBy(m => new { m.YearNumber, m.YearName })
                .OrderBy(g => g.Key.YearNumber)
                .Select(yearGroup =>
                {
                    var semesters = yearGroup
                        .GroupBy(m => new { m.SemesterId, m.SemesterNumber, m.SemesterName })
                        .OrderBy(g => g.Key.SemesterNumber)
                        .Select(semGroup =>
                        {
                            var modules = semGroup.Select(m =>
                            {
                                var hasAssessment = m.QuizQuestionCount > 0;
                                var isContentReady = m.PublishedLessonCount > 0 && m.LessonsWithAssets > 0;
                                var isAssessmentReady = hasAssessment;

                                if (isContentReady) modulesWithContent++;
                                if (isAssessmentReady) modulesWithAssessment++;

                                return new ModuleCatalogueResponse(
                                    m.Id,
                                    m.Title,
                                    m.LessonCount,
                                    m.PublishedLessonCount,
                                    m.LessonsWithAssets,
                                    hasAssessment,
                                    m.QuizQuestionCount,
                                    isContentReady,
                                    isAssessmentReady);
                            }).OrderBy(m => m.Title).ToList();

                            return new SemesterCatalogueResponse(
                                semGroup.Key.SemesterId,
                                semGroup.Key.SemesterNumber,
                                semGroup.Key.SemesterName,
                                modules.Count,
                                modules.Count(x => x.IsContentReady),
                                modules.Count(x => x.IsAssessmentReady),
                                modules);
                        }).ToList();

                    totalSemesters += semesters.Count;
                    return new YearCatalogueResponse(yearGroup.Key.YearNumber, yearGroup.Key.YearName, semesters);
                }).ToList();

            items.Add(new ProgrammeCatalogueItemResponse(
                programme.Id,
                programme.Name,
                programme.Code,
                programme.DurationYears,
                universities,
                years));
        }

        return new ProgrammeCatalogueResponse(
            items,
            programmes.Count,
            totalSemesters,
            totalModules,
            modulesWithContent,
            modulesWithAssessment,
            universityMap.Count);
    }

    public async Task DeleteAsync(Guid programmeId, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can delete programmes.");

        if (!await _db.Programmes.AsNoTracking().AnyAsync(p => p.Id == programmeId, cancellationToken))
            throw new NotFoundException("Programme not found.");

        var hasEnrolments = await _db.StudentEnrolments.AsNoTracking()
            .AnyAsync(e => e.Cohort.ProgrammeId == programmeId, cancellationToken);
        if (hasEnrolments)
            throw new ConflictException("Cannot delete programme while students are enrolled. Remove enrolments/cohorts first.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var lessonAssets = await _db.LessonAssets.AsNoTracking()
                .Where(a => a.Lesson.Module.Semester.ProgrammeYear.ProgrammeId == programmeId)
                .Select(a => a.BlobUrl)
                .ToListAsync(cancellationToken);

            foreach (var blobUrl in lessonAssets)
                await _blobStorage.DeleteAsync(blobUrl, cancellationToken);

            var lessonIds = await _db.Lessons.AsNoTracking()
                .Where(l => l.Module.Semester.ProgrammeYear.ProgrammeId == programmeId)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            var moduleIds = await _db.Modules.AsNoTracking()
                .Where(m => m.Semester.ProgrammeYear.ProgrammeId == programmeId)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var quizIds = await _db.Quizzes.AsNoTracking()
                .Where(q => moduleIds.Contains(q.ModuleId))
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var attemptIds = await _db.QuizAttempts.AsNoTracking()
                .Where(a => quizIds.Contains(a.QuizId))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            _db.QuizAnswers.RemoveRange(_db.QuizAnswers.Where(a => attemptIds.Contains(a.AttemptId)));
            _db.QuizAttempts.RemoveRange(_db.QuizAttempts.Where(a => quizIds.Contains(a.QuizId)));
            _db.QuizOptions.RemoveRange(_db.QuizOptions.Where(o => quizIds.Contains(o.Question.QuizId)));
            _db.QuizQuestions.RemoveRange(_db.QuizQuestions.Where(q => quizIds.Contains(q.QuizId)));
            _db.Quizzes.RemoveRange(_db.Quizzes.Where(q => quizIds.Contains(q.Id)));

            _db.LessonProgresses.RemoveRange(_db.LessonProgresses.Where(p => lessonIds.Contains(p.LessonId)));
            _db.ModuleProgresses.RemoveRange(_db.ModuleProgresses.Where(p => moduleIds.Contains(p.ModuleId)));

            _db.ContentPublications.RemoveRange(_db.ContentPublications.Where(p => lessonIds.Contains(p.LessonId)));
            _db.LessonAssets.RemoveRange(_db.LessonAssets.Where(a => lessonIds.Contains(a.LessonId)));
            _db.Lessons.RemoveRange(_db.Lessons.Where(l => lessonIds.Contains(l.Id)));

            _db.UniversityProgrammes.RemoveRange(_db.UniversityProgrammes.Where(up => up.ProgrammeId == programmeId));
            _db.Cohorts.RemoveRange(_db.Cohorts.Where(c => c.ProgrammeId == programmeId));

            _db.Modules.RemoveRange(_db.Modules.Where(m => moduleIds.Contains(m.Id)));
            _db.Semesters.RemoveRange(_db.Semesters.Where(s => s.ProgrammeYear.ProgrammeId == programmeId));
            _db.ProgrammeYears.RemoveRange(_db.ProgrammeYears.Where(y => y.ProgrammeId == programmeId));

            _db.Certificates.RemoveRange(_db.Certificates.Where(c => c.ProgrammeId == programmeId));
            _db.Programmes.RemoveRange(_db.Programmes.Where(p => p.Id == programmeId));

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteModuleAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can delete modules.");

        if (!await _db.Modules.AsNoTracking().AnyAsync(m => m.Id == moduleId, cancellationToken))
            throw new NotFoundException("Module not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var lessonAssets = await _db.LessonAssets.AsNoTracking()
                .Where(a => a.Lesson.ModuleId == moduleId)
                .Select(a => a.BlobUrl)
                .ToListAsync(cancellationToken);

            foreach (var blobUrl in lessonAssets)
                await _blobStorage.DeleteAsync(blobUrl, cancellationToken);

            var lessonIds = await _db.Lessons.AsNoTracking()
                .Where(l => l.ModuleId == moduleId)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            var quizIds = await _db.Quizzes.AsNoTracking()
                .Where(q => q.ModuleId == moduleId)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var attemptIds = await _db.QuizAttempts.AsNoTracking()
                .Where(a => quizIds.Contains(a.QuizId))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            _db.QuizAnswers.RemoveRange(_db.QuizAnswers.Where(a => attemptIds.Contains(a.AttemptId)));
            _db.QuizAttempts.RemoveRange(_db.QuizAttempts.Where(a => quizIds.Contains(a.QuizId)));
            _db.QuizOptions.RemoveRange(_db.QuizOptions.Where(o => quizIds.Contains(o.Question.QuizId)));
            _db.QuizQuestions.RemoveRange(_db.QuizQuestions.Where(q => quizIds.Contains(q.QuizId)));
            _db.Quizzes.RemoveRange(_db.Quizzes.Where(q => quizIds.Contains(q.Id)));

            _db.LessonProgresses.RemoveRange(_db.LessonProgresses.Where(p => lessonIds.Contains(p.LessonId)));
            _db.ModuleProgresses.RemoveRange(_db.ModuleProgresses.Where(p => p.ModuleId == moduleId));

            _db.ContentPublications.RemoveRange(_db.ContentPublications.Where(p => lessonIds.Contains(p.LessonId)));
            _db.LessonAssets.RemoveRange(_db.LessonAssets.Where(a => lessonIds.Contains(a.LessonId)));
            _db.Lessons.RemoveRange(_db.Lessons.Where(l => lessonIds.Contains(l.Id)));
            _db.Modules.RemoveRange(_db.Modules.Where(m => m.Id == moduleId));

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
