using CareTrack.Application.DTOs.Content;

namespace CareTrack.Application.Interfaces;

public interface IContentService
{
    Task<IReadOnlyList<ModulePickerResponse>> GetModulesAsync(CancellationToken cancellationToken = default);
    Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request, CancellationToken cancellationToken = default);
    Task<LessonResponse> GetLessonAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LessonResponse> UpdateLessonAsync(Guid id, UpdateLessonRequest request, CancellationToken cancellationToken = default);
    Task<LessonAssetResponse> UploadAssetAsync(Guid lessonId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<LessonResponse> UpdateStatusAsync(Guid id, UpdateLessonStatusRequest request, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid id, PublishLessonRequest request, CancellationToken cancellationToken = default);
    Task ApproveReviewAsync(Guid id, CancellationToken cancellationToken = default);
}
