using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Integration;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace CareTrack.Infrastructure.Services;

public class IntegrationService : IIntegrationService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(
        CareTrackDbContext db,
        ITenantContext tenant,
        INotificationService notifications,
        UserManager<ApplicationUser> userManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<IntegrationService> logger)
    {
        _db = db;
        _tenant = tenant;
        _notifications = notifications;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunSisRosterSyncAsync(Guid? universityId = null, CancellationToken cancellationToken = default)
    {
        var universities = universityId.HasValue
            ? await _db.Universities.Where(u => u.Id == universityId).ToListAsync(cancellationToken)
            : await _db.Universities.Where(u => u.IsActive).ToListAsync(cancellationToken);

        foreach (var university in universities)
        {
            var run = new SisRosterSyncRun { UniversityId = university.Id, Status = SyncRunStatus.Running };
            _db.SisRosterSyncRuns.Add(run);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var apiUrl = _configuration["Integrations:Sis:ApiBaseUrl"];
                if (string.IsNullOrWhiteSpace(apiUrl) || apiUrl.Contains("PLACEHOLDER"))
                {
                    run.Status = SyncRunStatus.Completed;
                    run.CompletedAt = DateTime.UtcNow;
                    run.RecordsProcessed = 0;
                    run.ErrorMessage = "SIS API not configured — placeholder mode";
                    await _db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var client = _httpClientFactory.CreateClient("Sis");
                var response = await client.GetAsync($"{apiUrl}/roster?university={university.Domain}", cancellationToken);
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<List<SisRosterDto>>(cancellationToken) ?? [];

                foreach (var row in payload)
                {
                    _db.SisRosterSyncRecords.Add(new SisRosterSyncRecord
                    {
                        SyncRunId = run.Id,
                        ExternalStudentId = row.ExternalId,
                        Action = row.Action,
                        StudentEmail = row.Email,
                        AppliedAt = DateTime.UtcNow,
                        Details = row.FullName
                    });
                    run.RecordsProcessed++;
                }

                run.Status = SyncRunStatus.Completed;
                run.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SIS roster sync failed for {UniversityId}", university.Id);
                run.Status = SyncRunStatus.Failed;
                run.ErrorMessage = ex.Message;
                run.CompletedAt = DateTime.UtcNow;

                var admins = await _userManager.Users
                    .Where(u => u.UniversityId == university.Id && u.Role == UserRole.UniversityAdmin)
                    .ToListAsync(cancellationToken);
                foreach (var admin in admins)
                    await _notifications.SendAsync(admin.Id, university.Id, "sync_failure", "SIS sync failed", ex.Message, "Email", run.Id, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<SisSyncRunResponse>> GetSisSyncHistoryAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SisRosterSyncRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .Take(20)
            .Select(r => new SisSyncRunResponse(r.Id, r.Status.ToString(), r.StartedAt, r.CompletedAt, r.RecordsProcessed, r.ErrorMessage))
            .ToListAsync(cancellationToken);
    }

    public async Task<GradeSyncRequestResponse> CreateGradeSyncRequestAsync(CreateGradeSyncRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.UniversityAdmin && !_tenant.IsApolloUser)
            throw new ForbiddenException("University admin required.");

        var universityId = _tenant.UniversityId ?? throw new ValidationException("University context required.");
        var cohort = await _db.Cohorts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");
        var semester = await _db.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SemesterId, cancellationToken)
            ?? throw new NotFoundException("Semester not found.");

        var syncRequest = new GradeSyncRequest
        {
            UniversityId = universityId,
            SemesterId = request.SemesterId,
            CohortId = request.CohortId,
            Status = GradeSyncStatus.PendingApproval,
            RequestedByUserId = _tenant.UserId ?? string.Empty
        };
        _db.GradeSyncRequests.Add(syncRequest);
        await _db.SaveChangesAsync(cancellationToken);

        return new GradeSyncRequestResponse(syncRequest.Id, syncRequest.Status.ToString(), cohort.Name, semester.Name, syncRequest.CreatedAt, null);
    }

    public async Task ApproveGradeSyncAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.UniversityAdmin)
            throw new ForbiddenException("Only university admin can approve grade sync.");

        var request = await _db.GradeSyncRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException("Grade sync request not found.");

        if (request.Status != GradeSyncStatus.PendingApproval)
            throw new ConflictException("Request is not pending approval.");

        request.Status = GradeSyncStatus.Approved;
        request.ApprovedByUserId = _tenant.UserId;
        request.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await PushGradeSyncAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<GradeSyncRequestResponse>> GetGradeSyncRequestsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.GradeSyncRequests.AsNoTracking()
            .Include(r => r.Cohort)
            .Include(r => r.Semester)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new GradeSyncRequestResponse(r.Id, r.Status.ToString(), r.Cohort.Name, r.Semester.Name, r.CreatedAt, r.ApprovedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessHospitalAttendanceFeedAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.HospitalAttendanceFeeds
            .Where(f => f.Status == FeedProcessingStatus.Received || f.Status == FeedProcessingStatus.Delayed)
            .OrderBy(f => f.ReceivedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var feed in pending)
        {
            feed.Status = FeedProcessingStatus.Processing;
            try
            {
                var doc = JsonDocument.Parse(feed.PayloadJson);
                var studentExternalId = doc.RootElement.GetProperty("studentExternalId").GetString() ?? "";
                var recordDate = DateOnly.Parse(doc.RootElement.GetProperty("date").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"));

                var assignment = await _db.RotationAssignments
                    .Include(a => a.Student)
                    .FirstOrDefaultAsync(a => a.Student.UserId == studentExternalId && a.Status == RotationStatus.Active, cancellationToken);

                if (assignment is not null)
                {
                    _db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        UniversityId = feed.UniversityId,
                        RotationAssignmentId = assignment.Id,
                        StudentId = assignment.StudentId,
                        RecordDate = recordDate,
                        Source = AttendanceSource.Feed,
                        CheckInAt = DateTime.UtcNow,
                        DurationMinutes = doc.RootElement.TryGetProperty("durationMinutes", out var d) ? d.GetInt32() : 480,
                        FeedStatus = FeedProcessingStatus.Processed
                    });
                }

                feed.Status = FeedProcessingStatus.Processed;
                feed.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                feed.Status = FeedProcessingStatus.Failed;
                feed.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Failed to process attendance feed {FeedId}", feed.Id);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReceiveHospitalFeedAsync(Guid universityId, HospitalFeedWebhookRequest request, CancellationToken cancellationToken = default)
    {
        _tenant.EnsureUniversityAccess(universityId);
        _db.HospitalAttendanceFeeds.Add(new HospitalAttendanceFeed
        {
            UniversityId = universityId,
            ExternalRecordId = request.ExternalRecordId,
            PayloadJson = request.PayloadJson,
            Status = FeedProcessingStatus.Received
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AttendanceFeedStatusResponse> GetAttendanceFeedStatusAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.HospitalAttendanceFeeds.CountAsync(f => f.Status == FeedProcessingStatus.Received, cancellationToken);
        var delayed = await _db.HospitalAttendanceFeeds.CountAsync(f => f.Status == FeedProcessingStatus.Delayed, cancellationToken);
        var last = await _db.HospitalAttendanceFeeds.Where(f => f.ProcessedAt != null)
            .OrderByDescending(f => f.ProcessedAt).Select(f => f.ProcessedAt).FirstOrDefaultAsync(cancellationToken);

        var status = delayed > 0 ? "Delayed — retrying" : pending > 0 ? "Processing" : "Healthy";
        return new AttendanceFeedStatusResponse(status, pending, delayed, last);
    }

    private async Task PushGradeSyncAsync(GradeSyncRequest request, CancellationToken cancellationToken)
    {
        var apiUrl = _configuration["Integrations:Sis:GradePushUrl"];
        try
        {
            if (string.IsNullOrWhiteSpace(apiUrl) || apiUrl.Contains("PLACEHOLDER"))
            {
                request.Status = GradeSyncStatus.Pushed;
                request.PushedAt = DateTime.UtcNow;
                request.FailureReason = "SIS grade push not configured — placeholder mode";
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var client = _httpClientFactory.CreateClient("Sis");
            var response = await client.PostAsJsonAsync(apiUrl, new { requestId = request.Id }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                request.Status = GradeSyncStatus.Pushed;
                request.PushedAt = DateTime.UtcNow;
            }
            else
            {
                request.Status = GradeSyncStatus.Failed;
                request.FailureReason = $"HTTP {(int)response.StatusCode}";
                request.RetryCount++;
            }
        }
        catch (Exception ex)
        {
            request.Status = GradeSyncStatus.Failed;
            request.FailureReason = ex.Message;
            request.RetryCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private sealed record SisRosterDto(string ExternalId, string Email, string FullName, string Action);
}
