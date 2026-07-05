using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Assessment;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareTrack.Infrastructure.Services;

public class AssessmentService : IAssessmentService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBlobStorageService _blobStorage;

    public AssessmentService(CareTrackDbContext db, ITenantContext tenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _tenant = tenant;
        _blobStorage = blobStorage;
        QuestPDF.Settings.License = LicenseType.Community;
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

        return new QuizAttemptResponse(
            attempt.Id,
            attempt.ScorePercent,
            attempt.Passed,
            attempt.AttemptNumber,
            attempt.SubmittedAt!.Value);
    }

    public async Task<IReadOnlyList<QuizAttemptResponse>> GetAttemptsAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        return await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.QuizId == quizId && a.StudentId == studentId && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new QuizAttemptResponse(a.Id, a.ScorePercent, a.Passed, a.AttemptNumber, a.SubmittedAt!.Value))
            .ToListAsync(cancellationToken);
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

        var cohort = await _db.Cohorts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == _tenant.CohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");

        var modules = await _db.Modules.AsNoTracking()
            .Where(m => m.Semester.ProgrammeYear.ProgrammeId == cohort.ProgrammeId)
            .Where(m => m.Semester.ProgrammeYear.YearNumber == cohort.CurrentYear)
            .Where(m => m.Semester.SemesterNumber == cohort.CurrentSemester)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        foreach (var moduleId in modules)
        {
            var moduleDone = await _db.ModuleProgresses.AnyAsync(p =>
                p.StudentId == studentId && p.ModuleId == moduleId && p.IsCompleted, cancellationToken);
            if (!moduleDone)
                return new SemesterCompletionResponse(false, "Complete all modules first.", null, null);

            var quizPassed = await _db.Quizzes.AsNoTracking()
                .Where(q => q.ModuleId == moduleId)
                .AllAsync(q => _db.QuizAttempts.Any(a =>
                    a.QuizId == q.Id && a.StudentId == studentId && a.Passed), cancellationToken);

            if (!quizPassed)
                return new SemesterCompletionResponse(false, "Pass all module quizzes first.", null, null);
        }

        var cohortEntity = await _db.Cohorts
            .Include(c => c.Programme)
            .FirstAsync(c => c.Id == cohort.Id, cancellationToken);

        var newSemester = cohortEntity.CurrentSemester + 1;
        var newYear = cohortEntity.CurrentYear;

        var maxSemester = await _db.Semesters.AsNoTracking()
            .Where(s => s.ProgrammeYear.ProgrammeId == cohort.ProgrammeId && s.ProgrammeYear.YearNumber == cohort.CurrentYear)
            .MaxAsync(s => (int?)s.SemesterNumber, cancellationToken) ?? 2;

        if (newSemester > maxSemester)
        {
            newSemester = 1;
            newYear++;
        }

        cohortEntity.CurrentSemester = newSemester;
        cohortEntity.CurrentYear = newYear;
        cohortEntity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        if (newYear > cohortEntity.Programme.DurationYears)
            await GenerateCertificateAsync(cancellationToken);

        return new SemesterCompletionResponse(true, "Semester completed. Next semester unlocked.", newYear, newSemester);
    }

    public async Task<CertificateResponse?> GenerateCertificateAsync(CancellationToken cancellationToken = default)
    {
        var studentId = GetStudentId();

        var student = await _db.Students.AsNoTracking()
            .Include(s => s.Enrolments).ThenInclude(e => e.Cohort).ThenInclude(c => c.Programme)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student not found.");

        var programme = student.Enrolments.First().Cohort.Programme;

        var existing = await _db.Certificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.ProgrammeId == programme.Id, cancellationToken);

        if (existing is not null)
            return new CertificateResponse(existing.Id, existing.CertificateNumber, existing.IssuedAt, existing.PdfBlobUrl);

        var certNumber = $"CT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        using var pdfStream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text("CareTrack Certificate").FontSize(24).Bold();
                    col.Item().PaddingVertical(20).AlignCenter().Text($"This certifies that {student.FirstName} {student.LastName}")
                        .FontSize(16);
                    col.Item().AlignCenter().Text($"has completed {programme.Name}").FontSize(14);
                    col.Item().PaddingTop(20).AlignCenter().Text($"Certificate No: {certNumber}").FontSize(10);
                    col.Item().AlignCenter().Text($"Issued: {DateTime.UtcNow:dd MMM yyyy}").FontSize(10);
                });
            });
        }).GeneratePdf(pdfStream);

        pdfStream.Position = 0;
        var blobUrl = await _blobStorage.UploadAsync(pdfStream, $"{certNumber}.pdf", "application/pdf", cancellationToken);

        var certificate = new Certificate
        {
            StudentId = studentId,
            ProgrammeId = programme.Id,
            CertificateNumber = certNumber,
            PdfBlobUrl = blobUrl
        };

        _db.Certificates.Add(certificate);
        await _db.SaveChangesAsync(cancellationToken);

        return new CertificateResponse(certificate.Id, certificate.CertificateNumber, certificate.IssuedAt, certificate.PdfBlobUrl);
    }

    private Guid GetStudentId()
    {
        if (_tenant.Role != UserRole.Student || !_tenant.StudentId.HasValue)
            throw new ForbiddenException("Student access only.");
        return _tenant.StudentId.Value;
    }
}
