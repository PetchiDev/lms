using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Assessment;
using CareTrack.Application.DTOs.Certificates;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class AssessmentService : IAssessmentService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICertificateService _certificateService;

    public AssessmentService(CareTrackDbContext db, ITenantContext tenant, ICertificateService certificateService)
    {
        _db = db;
        _tenant = tenant;
        _certificateService = certificateService;
    }

    public async Task<QuizResponse> GetQuizAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        var moduleCompleted = await _db.ModuleProgresses.AsNoTracking()
            .AnyAsync(p => p.StudentId == studentId && p.ModuleId == moduleId && p.IsCompleted, cancellationToken);

        if (!moduleCompleted)
            throw new ForbiddenException("Complete all lessons before attempting the quiz.");

        var quiz = await _db.Quizzes.AsNoTracking()
            .Where(q => q.ModuleId == moduleId && q.IsActive)
            .Select(q => new
            {
                q.Id,
                q.Title,
                q.PassPercentage,
                q.TimeLimitMinutes,
                q.MaxAttempts,
                Questions = q.Questions.OrderBy(x => x.SortOrder).Select(x => new QuizQuestionResponse(
                    x.Id,
                    x.QuestionText,
                    x.Points,
                    x.Options.OrderBy(o => o.SortOrder).Select(o => new QuizOptionResponse(o.Id, o.OptionText)).ToList()
                )).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Quiz not found for this module.");

        var attemptCount = await _db.QuizAttempts.CountAsync(a => a.QuizId == quiz.Id && a.StudentId == studentId, cancellationToken);

        return new QuizResponse(
            quiz.Id,
            quiz.Title,
            quiz.PassPercentage,
            quiz.TimeLimitMinutes,
            quiz.MaxAttempts,
            Math.Max(0, quiz.MaxAttempts - attemptCount),
            quiz.Questions);
    }

    public async Task<QuizAttemptResponse> SubmitAttemptAsync(Guid quizId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        var quiz = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken)
            ?? throw new NotFoundException("Quiz not found.");

        var attempts = await _db.QuizAttempts
            .Where(a => a.QuizId == quizId && a.StudentId == studentId)
            .OrderByDescending(a => a.AttemptNumber)
            .ToListAsync(cancellationToken);

        if (attempts.Count >= quiz.MaxAttempts)
            throw new ConflictException("Maximum attempts reached.");

        var lastAttempt = attempts.FirstOrDefault();
        if (lastAttempt?.SubmittedAt is not null &&
            lastAttempt.SubmittedAt.Value.AddHours(quiz.CooldownHours) > DateTime.UtcNow &&
            !lastAttempt.Passed)
        {
            throw new ConflictException($"Retry available after {quiz.CooldownHours} hour cooldown.");
        }

        var totalPoints = quiz.Questions.Sum(q => q.Points);
        var earnedPoints = 0;
        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            StudentId = studentId,
            AttemptNumber = attempts.Count + 1,
            StartedAt = DateTime.UtcNow
        };

        _db.QuizAttempts.Add(attempt);

        foreach (var answer in request.Answers)
        {
            var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId)
                ?? throw new ValidationException($"Invalid question {answer.QuestionId}");

            var option = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId)
                ?? throw new ValidationException($"Invalid option for question {answer.QuestionId}");

            if (option.IsCorrect) earnedPoints += question.Points;

            _db.QuizAnswers.Add(new QuizAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = question.Id,
                SelectedOptionId = option.Id,
                IsCorrect = option.IsCorrect
            });
        }

        attempt.ScorePercent = totalPoints == 0 ? 0 : (int)(earnedPoints * 100.0 / totalPoints);
        attempt.Passed = attempt.ScorePercent >= quiz.PassPercentage;
        attempt.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        CertificateResponse? certificate = null;
        if (attempt.Passed && attempt.ScorePercent >= 60)
            certificate = await _certificateService.GenerateForCurrentStudentAsync(cancellationToken);

        SemesterCompletionResponse? semesterAdvance = null;
        if (attempt.Passed)
            semesterAdvance = await TryAdvanceSemesterAsync(studentId, cancellationToken);

        return new QuizAttemptResponse(
            attempt.Id,
            attempt.ScorePercent,
            attempt.Passed,
            attempt.AttemptNumber,
            attempt.SubmittedAt!.Value,
            certificate,
            semesterAdvance);
    }

    public async Task<IReadOnlyList<QuizAttemptResponse>> GetAttemptsAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.QuizId == quizId && a.StudentId == studentId && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new QuizAttemptResponse(a.Id, a.ScorePercent, a.Passed, a.AttemptNumber, a.SubmittedAt!.Value))
            .ToListAsync(cancellationToken);

        return attempts;
    }

    public async Task RecordOfflineResultAsync(OfflineAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser && _tenant.Role != UserRole.UniversityAdmin)
            throw new ForbiddenException("Not authorized to enter offline results.");

        _db.OfflineAssessmentResults.Add(new OfflineAssessmentResult
        {
            StudentId = request.StudentId,
            ModuleId = request.ModuleId,
            AssessmentType = request.AssessmentType,
            ScorePercent = request.ScorePercent,
            Passed = request.ScorePercent >= 60,
            Notes = request.Notes,
            EnteredByUserId = _tenant.UserId ?? string.Empty
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SemesterCompletionResponse> CheckSemesterCompletionAsync(CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();
        return await TryAdvanceSemesterAsync(studentId, cancellationToken)
            ?? new SemesterCompletionResponse(false, "Complete all modules and pass all assessments for this semester first.", null, null);
    }

    private async Task<SemesterCompletionResponse?> TryAdvanceSemesterAsync(Guid studentId, CancellationToken cancellationToken)
    {
        if (!_tenant.UniversityId.HasValue)
            return null;

        var enrolment = await _db.StudentEnrolments
            .Include(e => e.Cohort).ThenInclude(c => c.Programme)
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.UniversityId == _tenant.UniversityId, cancellationToken);

        if (enrolment is null)
            return null;

        var programmeId = enrolment.Cohort.ProgrammeId;
        var currentYear = enrolment.CurrentYear;
        var currentSemester = enrolment.CurrentSemester;

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => m.Semester.ProgrammeYear.ProgrammeId == programmeId)
            .Where(m => m.Semester.ProgrammeYear.YearNumber == currentYear)
            .Where(m => m.Semester.SemesterNumber == currentSemester)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (modules.Count == 0)
            return null;

        foreach (var moduleId in modules)
        {
            var moduleDone = await _db.ModuleProgresses.AnyAsync(p =>
                p.StudentId == studentId && p.ModuleId == moduleId && p.IsCompleted, cancellationToken);
            if (!moduleDone)
                return null;

            var quizzes = await _db.Quizzes.AsNoTracking()
                .Where(q => q.ModuleId == moduleId && q.IsActive)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            foreach (var quizId in quizzes)
            {
                var passed = await _db.QuizAttempts.AnyAsync(a =>
                    a.QuizId == quizId && a.StudentId == studentId && a.Passed, cancellationToken);
                if (!passed)
                    return null;
            }
        }

        var maxSemester = await _db.Semesters.AsNoTracking()
            .Where(s => s.ProgrammeYear.ProgrammeId == programmeId && s.ProgrammeYear.YearNumber == currentYear)
            .MaxAsync(s => (int?)s.SemesterNumber, cancellationToken) ?? 2;

        var newSemester = currentSemester + 1;
        var newYear = currentYear;

        if (newSemester > maxSemester)
        {
            newSemester = 1;
            newYear++;
        }

        if (newYear > enrolment.Cohort.Programme.DurationYears)
        {
            await _certificateService.GenerateForCurrentStudentAsync(cancellationToken);
            return new SemesterCompletionResponse(
                true,
                "Congratulations! You have completed the entire programme.",
                currentYear,
                currentSemester);
        }

        enrolment.CurrentYear = newYear;
        enrolment.CurrentSemester = newSemester;
        enrolment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new SemesterCompletionResponse(
            true,
            $"Semester complete! Year {newYear} · Semester {newSemester} is now unlocked.",
            newYear,
            newSemester);
    }

    public Task<CertificateResponse?> GenerateCertificateAsync(CancellationToken cancellationToken = default)
        => _certificateService.GenerateForCurrentStudentAsync(cancellationToken);

    public async Task<ProgrammeAssessmentOverviewResponse> GetProgrammeOverviewAsync(Guid programmeId, CancellationToken cancellationToken = default)
    {
        EnsureApolloUser();

        var programme = await _db.Programmes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == programmeId, cancellationToken)
            ?? throw new NotFoundException("Programme not found.");

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => m.Semester.ProgrammeYear.ProgrammeId == programmeId)
            .OrderBy(m => m.Semester.ProgrammeYear.YearNumber)
            .ThenBy(m => m.Semester.SemesterNumber)
            .ThenBy(m => m.SortOrder)
            .Select(m => new
            {
                m.Id,
                m.Title,
                YearNumber = m.Semester.ProgrammeYear.YearNumber,
                m.Semester.SemesterNumber,
                SemesterName = m.Semester.Name,
                Quiz = m.Quizzes.OrderBy(q => q.CreatedAt).Select(q => new
                {
                    q.Id,
                    q.Title,
                    QuestionCount = q.Questions.Count,
                    q.IsActive,
                    AttemptCount = q.Attempts.Count
                }).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var summaries = modules.Select(m => new ModuleAssessmentSummaryResponse(
            m.Id,
            m.Title,
            m.YearNumber,
            m.SemesterNumber,
            m.SemesterName,
            m.Quiz?.Id,
            m.Quiz?.Title,
            m.Quiz?.QuestionCount ?? 0,
            m.Quiz?.IsActive ?? false,
            m.Quiz?.AttemptCount ?? 0)).ToList();

        return new ProgrammeAssessmentOverviewResponse(programme.Id, programme.Name, summaries);
    }

    public async Task<AdminQuizDetailResponse> GetAdminQuizAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        EnsureApolloUser();

        var module = await _db.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            ?? throw new NotFoundException("Module not found.");

        var quiz = await _db.Quizzes.AsNoTracking()
            .Where(q => q.ModuleId == moduleId)
            .OrderBy(q => q.CreatedAt)
            .Select(q => new
            {
                q.Id,
                q.Title,
                q.PassPercentage,
                q.TimeLimitMinutes,
                q.MaxAttempts,
                q.CooldownHours,
                q.IsActive,
                AttemptCount = q.Attempts.Count,
                Questions = q.Questions.OrderBy(x => x.SortOrder).Select(x => new AdminQuizQuestionResponse(
                    x.Id,
                    x.QuestionText,
                    x.Points,
                    x.SortOrder,
                    x.Options.OrderBy(o => o.SortOrder).Select(o => new AdminQuizOptionResponse(o.Id, o.OptionText, o.IsCorrect)).ToList()
                )).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (quiz is null)
        {
            return new AdminQuizDetailResponse(
                null,
                module.Id,
                module.Title,
                $"{module.Title} Assessment",
                60,
                30,
                3,
                24,
                true,
                0,
                false,
                []);
        }

        return new AdminQuizDetailResponse(
            quiz.Id,
            module.Id,
            module.Title,
            quiz.Title,
            quiz.PassPercentage,
            quiz.TimeLimitMinutes,
            quiz.MaxAttempts,
            quiz.CooldownHours,
            quiz.IsActive,
            quiz.AttemptCount,
            quiz.AttemptCount > 0,
            quiz.Questions);
    }

    public async Task<AdminQuizDetailResponse> UpsertQuizAsync(Guid moduleId, UpsertQuizRequest request, CancellationToken cancellationToken = default)
    {
        EnsureApolloUser();

        var module = await _db.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            ?? throw new NotFoundException("Module not found.");

        var quiz = await _db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.ModuleId == moduleId, cancellationToken);

        var attemptCount = quiz?.Attempts.Count ?? 0;
        ValidateUpsertRequest(request, requireQuestions: attemptCount == 0);

        if (quiz is null)
        {
            quiz = new Quiz { ModuleId = moduleId };
            _db.Quizzes.Add(quiz);
        }

        attemptCount = quiz.Attempts.Count;

        quiz.Title = request.Title.Trim();
        quiz.PassPercentage = request.PassPercentage;
        quiz.TimeLimitMinutes = request.TimeLimitMinutes;
        quiz.MaxAttempts = request.MaxAttempts;
        quiz.CooldownHours = request.CooldownHours;
        quiz.IsActive = request.IsActive;
        quiz.UpdatedAt = DateTime.UtcNow;

        if (attemptCount == 0)
        {
            if (quiz.Questions.Count > 0)
            {
                _db.QuizOptions.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
                _db.QuizQuestions.RemoveRange(quiz.Questions);
                quiz.Questions.Clear();
            }

            for (var i = 0; i < request.Questions.Count; i++)
            {
                var qReq = request.Questions[i];
                var question = new QuizQuestion
                {
                    QuestionText = qReq.QuestionText.Trim(),
                    Points = qReq.Points,
                    SortOrder = i + 1
                };

                for (var j = 0; j < qReq.Options.Count; j++)
                {
                    var oReq = qReq.Options[j];
                    question.Options.Add(new QuizOption
                    {
                        OptionText = oReq.OptionText.Trim(),
                        IsCorrect = oReq.IsCorrect,
                        SortOrder = j + 1
                    });
                }

                quiz.Questions.Add(question);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAdminQuizAsync(moduleId, cancellationToken);
    }

    private void EnsureApolloUser()
    {
        if (_tenant.Role is not (UserRole.ApolloAdmin or UserRole.ApolloFaculty))
            throw new ForbiddenException("Apollo staff access only.");
    }

    private static void ValidateUpsertRequest(UpsertQuizRequest request, bool requireQuestions)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Assessment title is required.");

        if (request.PassPercentage is < 1 or > 100)
            throw new ValidationException("Pass percentage must be between 1 and 100.");

        if (request.TimeLimitMinutes < 1)
            throw new ValidationException("Time limit must be at least 1 minute.");

        if (request.MaxAttempts < 1)
            throw new ValidationException("Max attempts must be at least 1.");

        if (!requireQuestions)
            return;

        if (request.Questions.Count == 0)
            throw new ValidationException("Add at least one question.");

        foreach (var question in request.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.QuestionText))
                throw new ValidationException("Every question must have text.");

            if (question.Options.Count < 2)
                throw new ValidationException("Each question needs at least two options.");

            if (question.Options.Count(o => o.IsCorrect) != 1)
                throw new ValidationException("Each question must have exactly one correct answer.");

            if (question.Options.Any(o => string.IsNullOrWhiteSpace(o.OptionText)))
                throw new ValidationException("Every option must have text.");
        }
    }

    private Guid GetStudentId()
    {
        if (_tenant.Role != UserRole.Student || !_tenant.StudentId.HasValue)
            throw new ForbiddenException("Student access only.");
        return _tenant.StudentId.Value;
    }
}
