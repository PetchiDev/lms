namespace CareTrack.Application.DTOs.Assessment;

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
    DateTime SubmittedAt);

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

public record CertificateResponse(
    Guid Id,
    string CertificateNumber,
    DateTime IssuedAt,
    string? PdfBlobUrl);
