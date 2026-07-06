using CareTrack.Application.DTOs.Learning;

namespace CareTrack.Application.Interfaces;

public interface ILearningService
{
    Task<StudentDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<ModuleDetailResponse> GetModuleAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<LessonDetailResponse> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task UpdateProgressAsync(Guid lessonId, UpdateLessonProgressRequest request, CancellationToken cancellationToken = default);
    Task<MarkLessonCompleteResponse> MarkCompleteAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<BulkCompleteResponse> MarkModuleLessonsCompleteAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<BulkCompleteResponse> MarkCurriculumCompleteAsync(CancellationToken cancellationToken = default);
}
