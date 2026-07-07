using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Universities;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class UniversityService : IUniversityService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IContentService _contentService;

    public UniversityService(
        CareTrackDbContext db,
        ITenantContext tenant,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IContentService contentService)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
        _emailService = emailService;
        _contentService = contentService;
    }

    public async Task<UniversityResponse> CreateAsync(CreateUniversityRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can create universities.");

        if (await _db.Universities.AnyAsync(u => u.Domain == request.Domain, cancellationToken))
            throw new ConflictException("University domain already exists.");

        var university = new University
        {
            Name = request.Name,
            Domain = request.Domain
        };

        _db.Universities.Add(university);

        if (request.ProgrammeId.HasValue)
        {
            _db.UniversityProgrammes.Add(new UniversityProgramme
            {
                UniversityId = university.Id,
                ProgrammeId = request.ProgrammeId.Value
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(university.Id, cancellationToken);
    }

    public async Task<UniversityResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(id);

        return await MapAsync(id, cancellationToken);
    }

    public async Task<PagedResult<UniversityResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Universities.AsNoTracking();

        if (!_tenant.IsApolloUser && _tenant.UniversityId.HasValue)
            query = query.Where(u => u.Id == _tenant.UniversityId);

        var total = await query.CountAsync(cancellationToken);
        var ids = await query.OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var items = new List<UniversityResponse>();
        foreach (var id in ids)
            items.Add(await MapAsync(id, cancellationToken));

        return new PagedResult<UniversityResponse>(items, total, page, pageSize);
    }

    public async Task<UniversityResponse> UpdateAsync(Guid id, UpdateUniversityRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can update universities.");

        var university = await _db.Universities.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        university.Name = request.Name;
        university.Domain = request.Domain;
        university.IsActive = request.IsActive;
        university.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<UniversityResponse> SetProgrammesAsync(Guid id, SetUniversityProgrammesRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can manage university programmes.");

        _ = await _db.Universities.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        var programmeIds = request.ProgrammeIds.Distinct().ToList();
        if (programmeIds.Count > 0)
        {
            var found = await _db.Programmes.AsNoTracking()
                .Where(p => programmeIds.Contains(p.Id))
                .CountAsync(cancellationToken);
            if (found != programmeIds.Count)
                throw new NotFoundException("One or more programmes were not found.");
        }

        var existing = await _db.UniversityProgrammes.Where(up => up.UniversityId == id).ToListAsync(cancellationToken);
        _db.UniversityProgrammes.RemoveRange(existing);

        foreach (var programmeId in programmeIds)
        {
            _db.UniversityProgrammes.Add(new UniversityProgramme
            {
                UniversityId = id,
                ProgrammeId = programmeId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var programmeId in programmeIds)
            await EnsureDefaultCohortAsync(id, programmeId, cancellationToken);

        foreach (var programmeId in programmeIds)
            await _contentService.PublishProgrammeLessonsToUniversityAsync(programmeId, id, cancellationToken);

        return await MapAsync(id, cancellationToken);
    }

    private async Task EnsureDefaultCohortAsync(Guid universityId, Guid programmeId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;

        var hasCohort = await _db.Cohorts.IgnoreQueryFilters()
            .AnyAsync(c => c.UniversityId == universityId && c.ProgrammeId == programmeId && c.IntakeYear == year, cancellationToken);

        if (hasCohort) return;

        var programmeCode = await _db.Programmes.AsNoTracking()
            .Where(p => p.Id == programmeId)
            .Select(p => p.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var label = string.IsNullOrWhiteSpace(programmeCode) ? "Programme" : programmeCode.Trim();

        _db.Cohorts.Add(new Cohort
        {
            UniversityId = universityId,
            ProgrammeId = programmeId,
            Name = $"{label} {year} Intake",
            IntakeYear = year,
            CurrentYear = 1,
            CurrentSemester = 1
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task InviteUniversityAdminAsync(CreateUniversityAdminRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can invite university admins.");

        _ = await _db.Universities.FindAsync([request.UniversityId], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new ConflictException("User already exists.");

        var token = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.UniversityAdmin,
            UniversityId = request.UniversityId,
            Status = EnrolmentStatus.Invited,
            InviteToken = token,
            InviteTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        await _userManager.CreateAsync(user);
        await _emailService.SendInviteEmailAsync(request.Email, user.FullName, token, cancellationToken);
    }

    public async Task<UniversityAdminResponse> CreateUniversityAdminAsync(
        CreateUniversityAdminDirectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can create university admins.");

        _ = await _db.Universities.FindAsync([request.UniversityId], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new ConflictException("User with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.UniversityAdmin,
            UniversityId = request.UniversityId,
            Status = EnrolmentStatus.Active,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ValidationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return new UniversityAdminResponse(user.Id, user.Email!, user.FullName, request.UniversityId);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can delete universities.");

        var university = await _db.Universities.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        var hasEnrolments = await _db.StudentEnrolments.AsNoTracking()
            .AnyAsync(e => e.UniversityId == id, cancellationToken);
        if (hasEnrolments)
            throw new ConflictException("Cannot delete university while students are enrolled. Remove enrolments first.");

        var hasUsers = await _userManager.Users.AsNoTracking()
            .AnyAsync(u => u.UniversityId == id, cancellationToken);
        if (hasUsers)
            throw new ConflictException("Cannot delete university while users exist for it. Delete users first.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.ContentPublications.RemoveRange(_db.ContentPublications.Where(p => p.UniversityId == id));
            _db.UniversityProgrammes.RemoveRange(_db.UniversityProgrammes.Where(up => up.UniversityId == id));
            _db.Cohorts.RemoveRange(_db.Cohorts.Where(c => c.UniversityId == id));

            _db.HospitalDepartments.RemoveRange(_db.HospitalDepartments.Where(x => x.UniversityId == id));
            _db.Supervisors.RemoveRange(_db.Supervisors.Where(x => x.UniversityId == id));
            _db.Rotations.RemoveRange(_db.Rotations.Where(x => x.UniversityId == id));
            _db.RotationAssignments.RemoveRange(_db.RotationAssignments.Where(x => x.UniversityId == id));
            _db.LogbookEntries.RemoveRange(_db.LogbookEntries.Where(x => x.UniversityId == id));
            _db.SignOffEscalations.RemoveRange(_db.SignOffEscalations.Where(x => x.UniversityId == id));
            _db.GradeSyncRequests.RemoveRange(_db.GradeSyncRequests.Where(x => x.UniversityId == id));
            _db.SisRosterSyncRuns.RemoveRange(_db.SisRosterSyncRuns.Where(x => x.UniversityId == id));
            _db.AttendanceRecords.RemoveRange(_db.AttendanceRecords.Where(x => x.UniversityId == id));
            _db.HospitalAttendanceFeeds.RemoveRange(_db.HospitalAttendanceFeeds.Where(x => x.UniversityId == id));
            _db.Notifications.RemoveRange(_db.Notifications.Where(x => x.UniversityId == id));
            _db.CalendarEvents.RemoveRange(_db.CalendarEvents.Where(x => x.UniversityId == id));
            _db.DiscussionThreads.RemoveRange(_db.DiscussionThreads.Where(x => x.UniversityId == id));
            _db.DiscussionPosts.RemoveRange(_db.DiscussionPosts.Where(x => x.Thread.UniversityId == id));

            _db.Universities.Remove(university);
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<UniversityResponse> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        var university = await _db.Universities.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Domain,
                u.IsActive,
                u.CreatedAt,
                ProgrammeIds = u.UniversityProgrammes.Select(p => p.ProgrammeId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("University not found.");

        return new UniversityResponse(
            university.Id,
            university.Name,
            university.Domain,
            university.IsActive,
            university.CreatedAt,
            university.ProgrammeIds);
    }
}
