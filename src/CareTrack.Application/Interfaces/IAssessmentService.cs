using CareTrack.Application.DTOs.Assessment;

namespace CareTrack.Application.Interfaces;

public interface IAssessmentService
{
    Task<QuizResponse> GetQuizAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<QuizAttemptResponse> SubmitAttemptAsync(Guid quizId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizAttemptResponse>> GetAttemptsAsync(Guid quizId, CancellationToken cancellationToken = default);
    Task RecordOfflineResultAsync(OfflineAssessmentRequest request, CancellationToken cancellationToken = default);
    Task<SemesterCompletionResponse> CheckSemesterCompletionAsync(CancellationToken cancellationToken = default);
    Task<CertificateResponse?> GenerateCertificateAsync(CancellationToken cancellationToken = default);
}
