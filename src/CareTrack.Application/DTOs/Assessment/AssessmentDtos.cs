namespace CareTrack.Application.DTOs.Assessment;

using CareTrack.Application.DTOs.Certificates;

public record QuizResponse(
    Guid Id,
    string Title,
    int PassPercentage,
    int TimeLimitMinutes,
    int MaxAttempts,
    int RemainingAttempts,
    IReadOnlyList<QuizQuestionResponse> Questions);

public record QuizQuestionResponse(
    Guid Id,
    string QuestionText,
    int Points,
    IReadOnlyList<QuizOptionResponse> Options);

public record QuizOptionResponse(Guid Id, string OptionText);

public record SubmitQuizAttemptRequest(IReadOnlyList<QuizAnswerRequest> Answers);

public record QuizAnswerRequest(Guid QuestionId, Guid SelectedOptionId);

public record QuizAttemptResponse(
    Guid Id,
    int ScorePercent,
    bool Passed,
    int AttemptNumber,
    DateTime SubmittedAt,
    CertificateResponse? Certificate = null,
    SemesterCompletionResponse? SemesterAdvance = null);

public record OfflineAssessmentRequest(
    Guid StudentId,
    Guid ModuleId,
    string AssessmentType,
    int ScorePercent,
    string? Notes);

public record SemesterCompletionResponse(
    bool Completed,
    string Message,
    int? NewYear,
    int? NewSemester);

public record ModuleAssessmentSummaryResponse(
    Guid ModuleId,
    string ModuleTitle,
    int YearNumber,
    int SemesterNumber,
    string SemesterName,
    Guid? QuizId,
    string? QuizTitle,
    int QuestionCount,
    bool IsActive,
    int AttemptCount);

public record ProgrammeAssessmentOverviewResponse(
    Guid ProgrammeId,
    string ProgrammeName,
    IReadOnlyList<ModuleAssessmentSummaryResponse> Modules);

public record AdminQuizOptionRequest(string OptionText, bool IsCorrect);

public record AdminQuizQuestionRequest(
    string QuestionText,
    int Points,
    IReadOnlyList<AdminQuizOptionRequest> Options);

public record UpsertQuizRequest(
    string Title,
    int PassPercentage,
    int TimeLimitMinutes,
    int MaxAttempts,
    int CooldownHours,
    bool IsActive,
    IReadOnlyList<AdminQuizQuestionRequest> Questions);

public record AdminQuizOptionResponse(Guid Id, string OptionText, bool IsCorrect);

public record AdminQuizQuestionResponse(
    Guid Id,
    string QuestionText,
    int Points,
    int SortOrder,
    IReadOnlyList<AdminQuizOptionResponse> Options);

public record AdminQuizDetailResponse(
    Guid? Id,
    Guid ModuleId,
    string ModuleTitle,
    string Title,
    int PassPercentage,
    int TimeLimitMinutes,
    int MaxAttempts,
    int CooldownHours,
    bool IsActive,
    int AttemptCount,
    bool QuestionsLocked,
    IReadOnlyList<AdminQuizQuestionResponse> Questions);
