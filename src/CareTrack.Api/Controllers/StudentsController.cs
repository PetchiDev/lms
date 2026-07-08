using Asp.Versioning;
using CareTrack.Application.DTOs.Clinical;
using CareTrack.Application.DTOs.Learning;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareTrack.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/students/me")]
[Route("api/{version:apiVersion}/students/me")]
[Authorize(Roles = nameof(UserRole.Student))]
public class StudentsController : ControllerBase
{
    private readonly ILearningService _learningService;
    private readonly IAssessmentService _assessmentService;
    private readonly IClinicalService _clinicalService;
    private readonly ICertificateService _certificateService;
    private readonly IMarksheetService _marksheetService;

    public StudentsController(
        ILearningService learningService,
        IAssessmentService assessmentService,
        IClinicalService clinicalService,
        ICertificateService certificateService,
        IMarksheetService marksheetService)
    {
        _learningService = learningService;
        _assessmentService = assessmentService;
        _clinicalService = clinicalService;
        _certificateService = certificateService;
        _marksheetService = marksheetService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(StudentDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _learningService.GetDashboardAsync(cancellationToken));

    [HttpGet("modules/{moduleId:guid}")]
    [ProducesResponseType(typeof(ModuleDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModuleDetailResponse>> GetModule(Guid moduleId, CancellationToken cancellationToken)
        => Ok(await _learningService.GetModuleAsync(moduleId, cancellationToken));

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LessonDetailResponse>> GetLesson(Guid lessonId, CancellationToken cancellationToken)
        => Ok(await _learningService.GetLessonAsync(lessonId, cancellationToken));

    [HttpPost("lessons/{lessonId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProgress(Guid lessonId, [FromBody] UpdateLessonProgressRequest request, CancellationToken cancellationToken)
    {
        await _learningService.UpdateProgressAsync(lessonId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("lessons/{lessonId:guid}/complete")]
    [ProducesResponseType(typeof(MarkLessonCompleteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MarkLessonCompleteResponse>> MarkComplete(Guid lessonId, CancellationToken cancellationToken)
        => Ok(await _learningService.MarkCompleteAsync(lessonId, cancellationToken));

    [HttpPost("modules/{moduleId:guid}/complete-all")]
    [ProducesResponseType(typeof(BulkCompleteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkCompleteResponse>> MarkModuleComplete(Guid moduleId, CancellationToken cancellationToken)
        => Ok(await _learningService.MarkModuleLessonsCompleteAsync(moduleId, cancellationToken));

    [HttpPost("curriculum/complete-all")]
    [ProducesResponseType(typeof(BulkCompleteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkCompleteResponse>> MarkCurriculumComplete(CancellationToken cancellationToken)
        => Ok(await _learningService.MarkCurriculumCompleteAsync(cancellationToken));

    [HttpGet("modules/{moduleId:guid}/quiz")]
    [ProducesResponseType(typeof(Application.DTOs.Assessment.QuizResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Application.DTOs.Assessment.QuizResponse>> GetQuiz(Guid moduleId, CancellationToken cancellationToken)
        => Ok(await _assessmentService.GetQuizAsync(moduleId, cancellationToken));

    [HttpPost("quizzes/{quizId:guid}/attempts")]
    [ProducesResponseType(typeof(Application.DTOs.Assessment.QuizAttemptResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Application.DTOs.Assessment.QuizAttemptResponse>> SubmitQuiz(Guid quizId, [FromBody] Application.DTOs.Assessment.SubmitQuizAttemptRequest request, CancellationToken cancellationToken)
        => Ok(await _assessmentService.SubmitAttemptAsync(quizId, request, cancellationToken));

    [HttpGet("quizzes/{quizId:guid}/attempts")]
    [ProducesResponseType(typeof(IReadOnlyList<Application.DTOs.Assessment.QuizAttemptResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Application.DTOs.Assessment.QuizAttemptResponse>>> GetAttempts(Guid quizId, CancellationToken cancellationToken)
        => Ok(await _assessmentService.GetAttemptsAsync(quizId, cancellationToken));

    /// <summary>Downloads a marksheet PDF for the latest submitted attempt of a quiz.</summary>
    [HttpGet("quizzes/{quizId:guid}/marksheet.pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadMarksheet(Guid quizId, CancellationToken cancellationToken)
    {
        var (pdfBytes, fileName) = await _marksheetService.RenderForCurrentStudentAsync(quizId, cancellationToken);
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost("semester/complete")]
    [ProducesResponseType(typeof(Application.DTOs.Assessment.SemesterCompletionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Application.DTOs.Assessment.SemesterCompletionResponse>> CompleteSemester(CancellationToken cancellationToken)
        => Ok(await _assessmentService.CheckSemesterCompletionAsync(cancellationToken));

    [HttpGet("certificates")]
    [ProducesResponseType(typeof(IReadOnlyList<Application.DTOs.Certificates.CertificateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Application.DTOs.Certificates.CertificateResponse>>> GetCertificates(CancellationToken cancellationToken)
        => Ok(await _certificateService.GetMyCertificatesAsync(cancellationToken));

    [HttpPost("certificate")]
    [ProducesResponseType(typeof(Application.DTOs.Certificates.CertificateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<Application.DTOs.Certificates.CertificateResponse>> GenerateCertificate(CancellationToken cancellationToken)
    {
        var result = await _certificateService.GenerateForCurrentStudentAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Student's active rotation assignments.</summary>
    [HttpGet("rotations")]
    [ProducesResponseType(typeof(IReadOnlyList<RotationAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RotationAssignmentResponse>>> GetMyRotations(CancellationToken cancellationToken)
        => Ok(await _clinicalService.GetMyRotationsAsync(cancellationToken));

    /// <summary>Creates a logbook entry for supervisor sign-off.</summary>
    [HttpPost("logbook")]
    [ProducesResponseType(typeof(LogbookEntryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LogbookEntryResponse>> CreateLogbookEntry([FromBody] CreateLogbookEntryRequest request, CancellationToken cancellationToken)
        => Ok(await _clinicalService.CreateLogbookEntryAsync(request, cancellationToken));

    /// <summary>Lists student's logbook entries.</summary>
    [HttpGet("logbook")]
    [ProducesResponseType(typeof(IReadOnlyList<LogbookEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LogbookEntryResponse>>> GetMyLogbookEntries(CancellationToken cancellationToken)
        => Ok(await _clinicalService.GetMyLogbookEntriesAsync(cancellationToken));
}
