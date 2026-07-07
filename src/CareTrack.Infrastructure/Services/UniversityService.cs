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
    private readonly IBlobStorageService _blobStorage;

    public UniversityService(
        CareTrackDbContext db,
        ITenantContext tenant,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IContentService contentService,
        IBlobStorageService blobStorage)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
        _emailService = emailService;
        _contentService = contentService;
        _blobStorage = blobStorage;
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
        await _emailService.SendInviteEmailAsync(request.Email, user.FullName, token, request.UniversityId, cancellationToken);
    }

    public async Task<UniversityResponse> UploadLogoAsync(
        Guid id,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can upload university logos.");

        var university = await _db.Universities.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new Domain.Exceptions.ValidationException("Logo must be an image file.");

        if (!string.IsNullOrWhiteSpace(university.LogoUrl))
            await _blobStorage.DeleteAsync(university.LogoUrl, cancellationToken);

        var cleanName = string.Concat(university.Name
            .Trim()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'))
            .Trim('_');
        if (string.IsNullOrWhiteSpace(cleanName))
            cleanName = "university";

        var ext = Path.GetExtension(fileName);
        var namedFile = $"{cleanName}{ext}";
        // Duplicates allowed: the blob service still prefixes with a GUID.
        university.LogoUrl = await _blobStorage.UploadAsync(fileStream, namedFile, contentType, $"media/universities/{cleanName}", cancellationToken);
        university.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await MapAsync(id, cancellationToken);
    }

    public async Task<UniversityEmailTemplateResponse> GetEmailTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(id);

        var template = await _db.Universities.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UniversityEmailTemplateResponse(
                u.EmailInviteSubject,
                u.EmailInviteBodyHtml,
                u.EmailFromName,
                u.EmailFromEmail))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("University not found.");

        return template;
    }

    public async Task<UniversityEmailTemplateResponse> UpdateEmailTemplateAsync(
        Guid id,
        UpdateUniversityEmailTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin && _tenant.Role != UserRole.UniversityAdmin)
            throw new ForbiddenException("Only admins can update email templates.");

        if (_tenant.Role == UserRole.UniversityAdmin)
            _tenant.EnsureUniversityAccess(id);

        var university = await _db.Universities.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        university.EmailInviteSubject = string.IsNullOrWhiteSpace(request.EmailInviteSubject) ? null : request.EmailInviteSubject.Trim();
        university.EmailInviteBodyHtml = string.IsNullOrWhiteSpace(request.EmailInviteBodyHtml) ? null : request.EmailInviteBodyHtml.Trim();
        university.EmailFromName = string.IsNullOrWhiteSpace(request.EmailFromName) ? null : request.EmailFromName.Trim();
        university.EmailFromEmail = string.IsNullOrWhiteSpace(request.EmailFromEmail) ? null : request.EmailFromEmail.Trim();
        university.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new UniversityEmailTemplateResponse(
            university.EmailInviteSubject,
            university.EmailInviteBodyHtml,
            university.EmailFromName,
            university.EmailFromEmail);
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

    public async Task<IReadOnlyList<UniversityAdminResponse>> GetUniversityAdminsAsync(
        Guid universityId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can list university admins.");

        if (!await _db.Universities.AsNoTracking().AnyAsync(u => u.Id == universityId, cancellationToken))
            throw new NotFoundException("University not found.");

        return await _userManager.Users.AsNoTracking()
            .Where(u => u.UniversityId == universityId && u.Role == UserRole.UniversityAdmin)
            .OrderBy(u => u.Email)
            .Select(u => new UniversityAdminResponse(u.Id, u.Email!, u.FullName, universityId))
            .ToListAsync(cancellationToken);
    }

    public async Task<UniversityAdminResponse> UpdateUniversityAdminAsync(
        UpdateUniversityAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can update university admins.");

        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        if (user.Role != UserRole.UniversityAdmin || user.UniversityId != request.UniversityId)
            throw new ForbiddenException("User is not an admin for this university.");

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (existing is not null && existing.Id != user.Id)
                throw new ConflictException("User with this email already exists.");

            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim();
            user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
            user.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new ValidationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
            if (!passwordResult.Succeeded)
                throw new ValidationException(string.Join("; ", passwordResult.Errors.Select(e => e.Description)));
        }

        return new UniversityAdminResponse(user.Id, user.Email!, user.FullName, request.UniversityId);
    }

    public async Task<DeleteAllUniversitiesResponse> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can delete universities.");

        var ids = await _db.Universities.AsNoTracking().Select(u => u.Id).ToListAsync(cancellationToken);
        var deleted = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var id in ids)
        {
            try
            {
                await DeleteAsync(id, cancellationToken);
                deleted++;
            }
            catch (Exception ex)
            {
                failed++;
                var name = await _db.Universities.AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                errors.Add($"{name ?? id.ToString()}: {ex.Message}");
            }
        }

        return new DeleteAllUniversitiesResponse(deleted, failed, errors);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.ApolloAdmin)
            throw new ForbiddenException("Only Apollo admin can delete universities.");

        var university = await _db.Universities.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("University not found.");

        if (!string.IsNullOrWhiteSpace(university.LogoUrl))
            await _blobStorage.DeleteAsync(university.LogoUrl, cancellationToken);

        var admins = await _userManager.Users
            .Where(u => u.UniversityId == id && u.Role == UserRole.UniversityAdmin)
            .ToListAsync(cancellationToken);
        foreach (var admin in admins)
            await _userManager.DeleteAsync(admin);

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
            _db.CertificateTemplates.RemoveRange(_db.CertificateTemplates.Where(t => t.UniversityId == id));

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
                u.LogoUrl,
                u.IsActive,
                u.CreatedAt,
                u.EmailInviteSubject,
                u.EmailInviteBodyHtml,
                ProgrammeIds = u.UniversityProgrammes.Select(p => p.ProgrammeId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("University not found.");

        var hasCustomEmail = !string.IsNullOrWhiteSpace(university.EmailInviteSubject)
            || !string.IsNullOrWhiteSpace(university.EmailInviteBodyHtml);

        return new UniversityResponse(
            university.Id,
            university.Name,
            university.Domain,
            university.LogoUrl,
            university.IsActive,
            university.CreatedAt,
            university.ProgrammeIds,
            hasCustomEmail);
    }
}
