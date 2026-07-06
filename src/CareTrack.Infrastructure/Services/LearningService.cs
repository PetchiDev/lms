using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Learning;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class LearningService : ILearningService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;

    public LearningService(CareTrackDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<StudentDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);
        var modules = await GetVisibleModulesAsync(ctx, cancellationToken);
        var moduleResponses = new List<EnrolledModuleResponse>();

        foreach (var module in modules)
        {
            var (locked, reason) = await IsModuleLockedAsync(ctx.StudentId, module.Id, cancellationToken);
            var progress = await GetModuleProgressPercentAsync(ctx.StudentId, module.Id, cancellationToken);
            moduleResponses.Add(new EnrolledModuleResponse(
                module.Id, module.Title, progress, locked, reason, progress >= 100));
        }

        var overall = moduleResponses.Count == 0 ? 0 : (int)moduleResponses.Average(m => m.ProgressPercent);

        return new StudentDashboardResponse(
            ctx.StudentName,
            ctx.CohortName,
            ctx.CurrentYear,
            ctx.CurrentSemester,
            overall,
            moduleResponses,
            [$"You are on Year {ctx.CurrentYear} · Semester {ctx.CurrentSemester}. Complete all modules and pass assessments to unlock the next semester."]);
    }

    public async Task<ModuleDetailResponse> GetModuleAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);
        var (locked, reason) = await IsModuleLockedAsync(ctx.StudentId, moduleId, cancellationToken);

        var module = await _db.Modules.AsNoTracking()
            .Where(m => m.Id == moduleId)
            .Select(m => new { m.Id, m.Title, m.Description })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Module not found.");

        var lessons = await _db.Lessons.AsNoTracking()
            .Where(l => l.ModuleId == moduleId && l.Status == ContentStatus.Published)
            .Where(l => _db.ContentPublications.Any(p =>
                p.LessonId == l.Id && (p.UniversityId == null || p.UniversityId == ctx.UniversityId)))
            .OrderBy(l => l.SortOrder)
            .Select(l => new { l.Id, l.Title })
            .ToListAsync(cancellationToken);

        var lessonResponses = new List<LessonSummaryResponse>();
        foreach (var lesson in lessons)
        {
            var progress = await _db.LessonProgresses.AsNoTracking()
                .Where(p => p.StudentId == ctx.StudentId && p.LessonId == lesson.Id)
                .Select(p => new { p.ProgressPercent, p.Status })
                .FirstOrDefaultAsync(cancellationToken);

            lessonResponses.Add(new LessonSummaryResponse(
                lesson.Id,
                lesson.Title,
                progress?.ProgressPercent ?? 0,
                progress?.Status.ToString() ?? LessonProgressStatus.NotStarted.ToString(),
                progress?.Status == LessonProgressStatus.Completed));
        }

        var moduleProgress = await GetModuleProgressPercentAsync(ctx.StudentId, moduleId, cancellationToken);

        return new ModuleDetailResponse(
            module.Id, module.Title, module.Description,
            moduleProgress, locked, reason, lessonResponses);
    }

    public async Task<LessonDetailResponse> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);

        var lesson = await _db.Lessons.AsNoTracking()
            .Where(l => l.Id == lessonId && l.Status == ContentStatus.Published)
            .Where(l => _db.ContentPublications.Any(p =>
                p.LessonId == l.Id && (p.UniversityId == null || p.UniversityId == ctx.UniversityId)))
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.Description,
                Assets = l.Assets.Select(a => new LessonAssetResponse(
                    a.Id, a.AssetType.ToString(), a.FileName, a.BlobUrl, a.ContentType)).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lesson not found or not available.");

        var progress = await _db.LessonProgresses.AsNoTracking()
            .Where(p => p.StudentId == ctx.StudentId && p.LessonId == lessonId)
            .Select(p => new { p.ProgressPercent, p.Status })
            .FirstOrDefaultAsync(cancellationToken);

        return new LessonDetailResponse(
            lesson.Id,
            lesson.Title,
            lesson.Description,
            progress?.ProgressPercent ?? 0,
            progress?.Status.ToString() ?? LessonProgressStatus.NotStarted.ToString(),
            lesson.Assets);
    }

    public async Task UpdateProgressAsync(Guid lessonId, UpdateLessonProgressRequest request, CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);
        _ = await GetLessonAsync(lessonId, cancellationToken);

        var progress = await _db.LessonProgresses
            .FirstOrDefaultAsync(p => p.StudentId == ctx.StudentId && p.LessonId == lessonId, cancellationToken);

        if (progress is null)
        {
            progress = new LessonProgress
            {
                StudentId = ctx.StudentId,
                LessonId = lessonId,
                Status = LessonProgressStatus.InProgress
            };
            _db.LessonProgresses.Add(progress);
        }

        progress.WatchedSeconds = Math.Max(progress.WatchedSeconds, request.WatchedSeconds);
        progress.ProgressPercent = Math.Max(progress.ProgressPercent, request.ProgressPercent);
        progress.Status = progress.ProgressPercent >= 90 ? LessonProgressStatus.InProgress : LessonProgressStatus.InProgress;
        progress.LastActivityAt = DateTime.UtcNow;
        progress.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await UpdateModuleProgressAsync(ctx.StudentId, lessonId, cancellationToken);
    }

    public async Task<MarkLessonCompleteResponse> MarkCompleteAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);

        var progress = await _db.LessonProgresses
            .FirstOrDefaultAsync(p => p.StudentId == ctx.StudentId && p.LessonId == lessonId, cancellationToken);

        if (progress is null || progress.ProgressPercent < 90)
        {
            await CompleteLessonAsync(ctx.StudentId, lessonId, cancellationToken);
            return new MarkLessonCompleteResponse(true, null);
        }

        progress.Status = LessonProgressStatus.Completed;
        progress.ProgressPercent = 100;
        progress.CompletedAt = DateTime.UtcNow;
        progress.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await UpdateModuleProgressAsync(ctx.StudentId, lessonId, cancellationToken);

        return new MarkLessonCompleteResponse(true, null);
    }

    public async Task<BulkCompleteResponse> MarkModuleLessonsCompleteAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);
        _ = await GetModuleAsync(moduleId, cancellationToken);

        var lessonIds = await GetPublishedLessonIdsAsync(moduleId, ctx.UniversityId, cancellationToken);
        foreach (var lessonId in lessonIds)
            await CompleteLessonAsync(ctx.StudentId, lessonId, cancellationToken);

        return await BuildBulkCompleteResponseAsync(ctx, moduleId, cancellationToken);
    }

    public async Task<BulkCompleteResponse> MarkCurriculumCompleteAsync(CancellationToken cancellationToken = default)
    {
        var ctx = await GetStudentContextAsync(cancellationToken);
        var modules = await GetVisibleModulesAsync(ctx, cancellationToken);

        foreach (var module in modules)
        {
            var lessonIds = await GetPublishedLessonIdsAsync(module.Id, ctx.UniversityId, cancellationToken);
            foreach (var lessonId in lessonIds)
                await CompleteLessonAsync(ctx.StudentId, lessonId, cancellationToken);
        }

        return await BuildBulkCompleteResponseAsync(ctx, null, cancellationToken);
    }

    private async Task CompleteLessonAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken)
    {
        var progress = await _db.LessonProgresses
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LessonId == lessonId, cancellationToken);

        if (progress is null)
        {
            progress = new LessonProgress { StudentId = studentId, LessonId = lessonId };
            _db.LessonProgresses.Add(progress);
        }

        progress.Status = LessonProgressStatus.Completed;
        progress.ProgressPercent = 100;
        progress.CompletedAt = DateTime.UtcNow;
        progress.LastActivityAt = DateTime.UtcNow;
        progress.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await UpdateModuleProgressAsync(studentId, lessonId, cancellationToken);
    }

    private async Task<List<Guid>> GetPublishedLessonIdsAsync(Guid moduleId, Guid universityId, CancellationToken cancellationToken)
    {
        return await _db.Lessons.AsNoTracking()
            .Where(l => l.ModuleId == moduleId && l.Status == ContentStatus.Published)
            .Where(l => _db.ContentPublications.Any(p =>
                p.LessonId == l.Id && (p.UniversityId == null || p.UniversityId == universityId)))
            .OrderBy(l => l.SortOrder)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<BulkCompleteResponse> BuildBulkCompleteResponseAsync(
        StudentContext ctx,
        Guid? moduleId,
        CancellationToken cancellationToken)
    {
        var modules = await GetVisibleModulesAsync(ctx, cancellationToken);
        var modulePercents = new List<int>();

        foreach (var module in modules)
        {
            var percent = await GetModuleProgressPercentAsync(ctx.StudentId, module.Id, cancellationToken);
            modulePercents.Add(percent);
        }

        var overall = modulePercents.Count == 0 ? 0 : (int)modulePercents.Average();
        var allComplete = modulePercents.Count > 0 && modulePercents.All(p => p >= 100);
        var modulePercent = moduleId.HasValue
            ? await GetModuleProgressPercentAsync(ctx.StudentId, moduleId.Value, cancellationToken)
            : overall;

        return new BulkCompleteResponse(
            modulePercent,
            modulePercent >= 100,
            allComplete,
            overall);
    }

    private async Task UpdateModuleProgressAsync(Guid studentId, Guid lessonId, CancellationToken cancellationToken)
    {
        var moduleId = await _db.Lessons.AsNoTracking()
            .Where(l => l.Id == lessonId)
            .Select(l => l.ModuleId)
            .FirstAsync(cancellationToken);

        var totalLessons = await _db.Lessons.CountAsync(l =>
            l.ModuleId == moduleId && l.Status == ContentStatus.Published, cancellationToken);

        var completedLessons = await _db.LessonProgresses.CountAsync(p =>
            p.StudentId == studentId && p.Lesson.ModuleId == moduleId && p.Status == LessonProgressStatus.Completed, cancellationToken);

        var percent = totalLessons == 0 ? 0 : (int)(completedLessons * 100.0 / totalLessons);

        var moduleProgress = await _db.ModuleProgresses
            .FirstOrDefaultAsync(p => p.StudentId == studentId && p.ModuleId == moduleId, cancellationToken);

        if (moduleProgress is null)
        {
            moduleProgress = new ModuleProgress { StudentId = studentId, ModuleId = moduleId };
            _db.ModuleProgresses.Add(moduleProgress);
        }

        moduleProgress.ProgressPercent = percent;
        moduleProgress.IsCompleted = percent >= 100;
        moduleProgress.CompletedAt = moduleProgress.IsCompleted ? DateTime.UtcNow : null;
        moduleProgress.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GetModuleProgressPercentAsync(Guid studentId, Guid moduleId, CancellationToken cancellationToken)
    {
        return await _db.ModuleProgresses.AsNoTracking()
            .Where(p => p.StudentId == studentId && p.ModuleId == moduleId)
            .Select(p => p.ProgressPercent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<(bool Locked, string? Reason)> IsModuleLockedAsync(Guid studentId, Guid moduleId, CancellationToken cancellationToken)
    {
        var prerequisites = await _db.ModulePrerequisites.AsNoTracking()
            .Where(p => p.ModuleId == moduleId)
            .Select(p => p.PrerequisiteModuleId)
            .ToListAsync(cancellationToken);

        if (prerequisites.Count == 0)
            return (false, null);

        foreach (var prereqId in prerequisites)
        {
            var completed = await _db.ModuleProgresses.AsNoTracking()
                .AnyAsync(p => p.StudentId == studentId && p.ModuleId == prereqId && p.IsCompleted, cancellationToken);

            if (!completed)
            {
                var title = await _db.Modules.AsNoTracking()
                    .Where(m => m.Id == prereqId)
                    .Select(m => m.Title)
                    .FirstAsync(cancellationToken);
                return (true, $"Unlocks after {title}");
            }
        }

        return (false, null);
    }

    private async Task<List<Module>> GetVisibleModulesAsync(StudentContext ctx, CancellationToken cancellationToken)
    {
        return await _db.Modules.AsNoTracking()
            .Where(m => m.Semester.ProgrammeYear.YearNumber <= ctx.CurrentYear)
            .Where(m => m.Semester.SemesterNumber <= ctx.CurrentSemester || m.Semester.ProgrammeYear.YearNumber < ctx.CurrentYear)
            .Where(m => m.Semester.ProgrammeYear.ProgrammeId == ctx.ProgrammeId)
            .OrderBy(m => m.Semester.ProgrammeYear.YearNumber)
            .ThenBy(m => m.Semester.SemesterNumber)
            .ThenBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
    }

    private async Task<StudentContext> GetStudentContextAsync(CancellationToken cancellationToken)
    {
        if (_tenant.Role != UserRole.Student || !_tenant.StudentId.HasValue || !_tenant.UniversityId.HasValue || !_tenant.CohortId.HasValue)
            throw new ForbiddenException("Student access only.");

        var data = await _db.StudentEnrolments.AsNoTracking()
            .Where(e => e.StudentId == _tenant.StudentId && e.UniversityId == _tenant.UniversityId)
            .Select(e => new StudentContext(
                e.StudentId,
                e.UniversityId,
                e.Cohort.ProgrammeId,
                e.Cohort.Name,
                e.CurrentYear,
                e.CurrentSemester,
                e.Student.FirstName + " " + e.Student.LastName))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Student enrolment not found.");

        return data;
    }

    private sealed record StudentContext(
        Guid StudentId,
        Guid UniversityId,
        Guid ProgrammeId,
        string CohortName,
        int CurrentYear,
        int CurrentSemester,
        string StudentName);
}
