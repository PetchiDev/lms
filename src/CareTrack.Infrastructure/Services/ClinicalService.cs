using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Clinical;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CareTrack.Infrastructure.Services;

public class ClinicalService : IClinicalService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly int _escalationDays;

    public ClinicalService(
        CareTrackDbContext db,
        ITenantContext tenant,
        INotificationService notifications,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _db = db;
        _tenant = tenant;
        _notifications = notifications;
        _userManager = userManager;
        _escalationDays = configuration.GetValue("Clinical:EscalationDays", 7);
    }

    public async Task<IReadOnlyList<HospitalDepartmentResponse>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.HospitalDepartments.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new HospitalDepartmentResponse(d.Id, d.Name, d.Code, d.CapacityPerMonth))
            .ToListAsync(cancellationToken);
    }

    public async Task<HospitalDepartmentResponse> CreateDepartmentAsync(string name, string code, int capacity, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo coordinators can create departments.");

        var universityId = _tenant.UniversityId
            ?? throw new ValidationException("University context required.");

        var dept = new HospitalDepartment
        {
            UniversityId = universityId,
            Name = name,
            Code = code,
            CapacityPerMonth = capacity
        };
        _db.HospitalDepartments.Add(dept);
        await _db.SaveChangesAsync(cancellationToken);
        return new HospitalDepartmentResponse(dept.Id, dept.Name, dept.Code, dept.CapacityPerMonth);
    }

    public async Task<IReadOnlyList<RotationResponse>> GetRotationsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Rotations.AsNoTracking()
            .Include(r => r.HospitalDepartment)
            .Include(r => r.Assignments)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new RotationResponse(
                r.Id, r.Name, r.HospitalDepartment.Name, r.StartDate, r.EndDate,
                r.Status.ToString(), r.Assignments.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<RotationResponse> CreateRotationAsync(CreateRotationRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo coordinators can schedule rotations.");

        var cohort = await _db.Cohorts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");

        var rotation = new Rotation
        {
            UniversityId = cohort.UniversityId,
            HospitalDepartmentId = request.HospitalDepartmentId,
            CohortId = request.CohortId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RequiredProcedureCount = request.RequiredProcedureCount,
            WeeksDuration = (request.EndDate.DayNumber - request.StartDate.DayNumber) / 7,
            Status = RotationStatus.Scheduled
        };
        _db.Rotations.Add(rotation);
        await _db.SaveChangesAsync(cancellationToken);

        var dept = await _db.HospitalDepartments.AsNoTracking().FirstAsync(d => d.Id == request.HospitalDepartmentId, cancellationToken);
        return new RotationResponse(rotation.Id, rotation.Name, dept.Name, rotation.StartDate, rotation.EndDate, rotation.Status.ToString(), 0);
    }

    public async Task AssignStudentsAsync(Guid rotationId, AssignStudentsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo coordinators can assign students.");

        var rotation = await _db.Rotations.FirstOrDefaultAsync(r => r.Id == rotationId, cancellationToken)
            ?? throw new NotFoundException("Rotation not found.");

        foreach (var studentId in request.StudentIds)
        {
            if (await _db.RotationAssignments.AnyAsync(a => a.RotationId == rotationId && a.StudentId == studentId, cancellationToken))
                continue;

            _db.RotationAssignments.Add(new RotationAssignment
            {
                UniversityId = rotation.UniversityId,
                RotationId = rotationId,
                StudentId = studentId,
                Status = RotationStatus.Active
            });
        }

        rotation.Status = RotationStatus.Active;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RotationAssignmentResponse>> GetMyRotationsAsync(CancellationToken cancellationToken = default)
    {
        var studentId = _tenant.StudentId
            ?? throw new ForbiddenException("Student access only.");

        return await _db.RotationAssignments.AsNoTracking()
            .Include(a => a.Rotation).ThenInclude(r => r.HospitalDepartment)
            .Include(a => a.Student)
            .Where(a => a.StudentId == studentId)
            .Select(a => new RotationAssignmentResponse(
                a.Id, a.RotationId, a.Rotation.Name,
                a.Student.FirstName + " " + a.Student.LastName,
                a.AttendancePercent, a.CompletedProcedureCount, a.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<LogbookEntryResponse> CreateLogbookEntryAsync(CreateLogbookEntryRequest request, CancellationToken cancellationToken = default)
    {
        var studentId = _tenant.StudentId
            ?? throw new ForbiddenException("Student access only.");

        var assignment = await _db.RotationAssignments
            .Include(a => a.Student)
            .Include(a => a.Rotation)
            .FirstOrDefaultAsync(a => a.Id == request.RotationAssignmentId && a.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("Rotation assignment not found.");

        var entry = new LogbookEntry
        {
            UniversityId = assignment.UniversityId,
            RotationAssignmentId = assignment.Id,
            StudentId = studentId,
            EntryDate = request.EntryDate,
            Procedure = request.Procedure,
            PatientCount = request.PatientCount,
            Notes = request.Notes,
            Location = request.Location,
            Status = LogbookEntryStatus.PendingSignoff,
            SubmittedAt = DateTime.UtcNow
        };
        _db.LogbookEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        var supervisors = await _db.Supervisors.AsNoTracking()
            .Where(s => s.HospitalDepartmentId == assignment.Rotation.HospitalDepartmentId && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var sup in supervisors)
        {
            var user = await _userManager.FindByIdAsync(sup.UserId);
            if (user is not null)
                await _notifications.SendAsync(user.Id, assignment.UniversityId, "logbook_pending",
                    "New logbook entry", $"{assignment.Student.FirstName} submitted a logbook entry for sign-off.", "InApp", entry.Id, cancellationToken);
        }

        return MapEntry(entry, assignment.Student.FirstName + " " + assignment.Student.LastName);
    }

    public async Task<IReadOnlyList<LogbookEntryResponse>> GetMyLogbookEntriesAsync(CancellationToken cancellationToken = default)
    {
        var studentId = _tenant.StudentId
            ?? throw new ForbiddenException("Student access only.");

        var entries = await _db.LogbookEntries.AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync(cancellationToken);

        return entries.Select(e => MapEntry(e, e.Student.FirstName + " " + e.Student.LastName)).ToList();
    }

    public async Task<SupervisorDashboardResponse> GetSupervisorDashboardAsync(CancellationToken cancellationToken = default)
    {
        var supervisorId = _tenant.SupervisorId
            ?? throw new ForbiddenException("Supervisor access only.");

        var supervisor = await _db.Supervisors.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supervisorId, cancellationToken)
            ?? throw new NotFoundException("Supervisor not found.");

        var delegateIds = await _db.SupervisorDelegations.AsNoTracking()
            .Where(d => d.DelegateSupervisorId == supervisorId && d.IsActive
                && d.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow)
                && d.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            .Select(d => d.SupervisorId)
            .ToListAsync(cancellationToken);

        var supervisorIds = delegateIds.Append(supervisorId).ToList();
        var deptId = supervisor.HospitalDepartmentId;

        var pendingQuery = _db.LogbookEntries.AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.RotationAssignment).ThenInclude(a => a.Rotation)
            .Where(e => e.RotationAssignment.Rotation.HospitalDepartmentId == deptId
                && (e.Status == LogbookEntryStatus.PendingSignoff || e.Status == LogbookEntryStatus.Escalated));

        var pending = await pendingQuery.OrderBy(e => e.SubmittedAt).Take(50).ToListAsync(cancellationToken);
        var escalatedCount = pending.Count(e => e.Status == LogbookEntryStatus.Escalated);
        var approvedToday = await _db.LogbookEntries.CountAsync(e =>
            e.SupervisorId == supervisorId && e.ReviewedAt != null && e.ReviewedAt >= DateTime.UtcNow.Date, cancellationToken);

        return new SupervisorDashboardResponse(
            pending.Count(e => e.Status == LogbookEntryStatus.PendingSignoff),
            escalatedCount,
            approvedToday,
            pending.Select(e => MapEntry(e, e.Student.FirstName + " " + e.Student.LastName)).ToList());
    }

    public async Task<LogbookEntryResponse> SignOffEntryAsync(Guid entryId, SignOffRequest request, CancellationToken cancellationToken = default)
    {
        var supervisorId = _tenant.SupervisorId
            ?? throw new ForbiddenException("Supervisor access only.");

        var entry = await _db.LogbookEntries
            .Include(e => e.Student)
            .Include(e => e.RotationAssignment)
            .FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken)
            ?? throw new NotFoundException("Logbook entry not found.");

        if (request.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
        {
            entry.Status = LogbookEntryStatus.Approved;
            entry.SupervisorId = supervisorId;
            entry.SupervisorRemarks = request.Remarks;
            entry.ReviewedAt = DateTime.UtcNow;

            var assignment = entry.RotationAssignment;
            assignment.CompletedProcedureCount++;
            await UpdateRotationCompletionAsync(assignment, cancellationToken);

            var studentUser = await _userManager.Users.FirstOrDefaultAsync(u => u.StudentId == entry.StudentId, cancellationToken);
            if (studentUser is not null)
                await _notifications.SendAsync(studentUser.Id, entry.UniversityId, "logbook_approved",
                    "Logbook approved", $"Your entry for {entry.Procedure} was approved.", "InApp", entry.Id, cancellationToken);
        }
        else if (request.Action.Equals("reject", StringComparison.OrdinalIgnoreCase))
        {
            entry.Status = LogbookEntryStatus.Rejected;
            entry.SupervisorId = supervisorId;
            entry.SupervisorRemarks = request.Remarks;
            entry.ReviewedAt = DateTime.UtcNow;

            var studentUser = await _userManager.Users.FirstOrDefaultAsync(u => u.StudentId == entry.StudentId, cancellationToken);
            if (studentUser is not null)
                await _notifications.SendAsync(studentUser.Id, entry.UniversityId, "logbook_rejected",
                    "Logbook rejected", request.Remarks ?? "Please revise and resubmit.", "InApp", entry.Id, cancellationToken);
        }
        else
        {
            throw new ValidationException("Action must be approve or reject.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapEntry(entry, entry.Student.FirstName + " " + entry.Student.LastName);
    }

    public async Task ProcessEscalationsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_escalationDays);
        var stale = await _db.LogbookEntries
            .IgnoreQueryFilters()
            .Include(e => e.RotationAssignment).ThenInclude(a => a.Rotation)
            .Where(e => e.Status == LogbookEntryStatus.PendingSignoff && e.SubmittedAt <= cutoff && e.EscalatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var entry in stale)
        {
            var coordinator = await _userManager.Users
                .Where(u => u.UniversityId == entry.UniversityId && u.Role == UserRole.UniversityAdmin)
                .FirstOrDefaultAsync(cancellationToken);

            if (coordinator is null) continue;

            entry.Status = LogbookEntryStatus.Escalated;
            entry.EscalatedAt = DateTime.UtcNow;
            _db.SignOffEscalations.Add(new SignOffEscalation
            {
                UniversityId = entry.UniversityId,
                LogbookEntryId = entry.Id,
                EscalatedToUserId = coordinator.Id,
                Reason = $"Pending sign-off for {_escalationDays}+ days"
            });

            await _notifications.SendAsync(coordinator.Id, entry.UniversityId, "signoff_escalation",
                "Sign-off escalation", $"Logbook entry pending {_escalationDays} days — action required.", "Email", entry.Id, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateRotationCompletionAsync(RotationAssignment assignment, CancellationToken cancellationToken)
    {
        var rotation = await _db.Rotations.AsNoTracking().FirstAsync(r => r.Id == assignment.RotationId, cancellationToken);
        var signedCount = await _db.LogbookEntries.CountAsync(e =>
            e.RotationAssignmentId == assignment.Id && e.Status == LogbookEntryStatus.Approved, cancellationToken);

        if (assignment.AttendancePercent >= rotation.RequiredAttendancePercent
            && signedCount >= rotation.RequiredProcedureCount)
        {
            assignment.Status = RotationStatus.Completed;
            assignment.CompletedAt = DateTime.UtcNow;
        }
    }

    private static LogbookEntryResponse MapEntry(LogbookEntry e, string studentName) =>
        new(e.Id, e.EntryDate, e.Procedure, e.PatientCount, e.Notes, e.Location,
            e.Status.ToString(), studentName, e.SupervisorRemarks, e.SubmittedAt, e.ReviewedAt, e.EscalatedAt != null);
}
