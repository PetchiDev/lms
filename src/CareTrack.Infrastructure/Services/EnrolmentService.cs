using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Enrolment;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CareTrack.Infrastructure.Services;

public class EnrolmentService : IEnrolmentService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly UserManager<ApplicationUser> _userManager;

    public EnrolmentService(
        CareTrackDbContext db,
        ITenantContext tenant,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _tenant = tenant;
        _userManager = userManager;
    }

    public async Task<StudentEnrolmentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
    {
        var cohort = await _db.Cohorts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");

        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(cohort.UniversityId);

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new ConflictException("User with this email already exists.");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var studentId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = UserRole.Student,
                UniversityId = cohort.UniversityId,
                CohortId = cohort.Id,
                StudentId = studentId,
                Status = EnrolmentStatus.Active,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                throw new Domain.Exceptions.ValidationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            var student = new Student
            {
                Id = studentId,
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            _db.Students.Add(student);
            _db.StudentEnrolments.Add(new StudentEnrolment
            {
                UniversityId = cohort.UniversityId,
                StudentId = student.Id,
                CohortId = cohort.Id,
                Status = EnrolmentStatus.Active,
                ActivatedAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await MapStudentAsync(student.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CsvImportResult> ImportStudentsAsync(Stream csvStream, Guid cohortId, CancellationToken cancellationToken = default)
    {
        var cohort = await _db.Cohorts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");

        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(cohort.UniversityId);

        var errors = new List<string>();
        var success = 0;
        var total = 0;

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null });

        await foreach (var record in csv.GetRecordsAsync<CsvStudentRow>(cancellationToken))
        {
            total++;
            try
            {
                await CreateStudentAsync(
                    new CreateStudentRequest(
                        record.Email,
                        record.FirstName,
                        record.LastName,
                        cohortId,
                        string.IsNullOrWhiteSpace(record.Password) ? "Student@123" : record.Password),
                    cancellationToken);
                success++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {total} ({record.Email}): {ex.Message}");
            }
        }

        return new CsvImportResult(total, success, total - success, errors);
    }

    public async Task<PagedResult<StudentEnrolmentResponse>> GetStudentsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.StudentEnrolments.AsNoTracking();

        if (!_tenant.IsApolloUser && _tenant.UniversityId.HasValue)
            query = query.Where(e => e.UniversityId == _tenant.UniversityId);

        var total = await query.CountAsync(cancellationToken);
        var studentIds = await query.OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        var items = new List<StudentEnrolmentResponse>();
        foreach (var id in studentIds)
            items.Add(await MapStudentAsync(id, cancellationToken));

        return new PagedResult<StudentEnrolmentResponse>(items, total, page, pageSize);
    }

    public async Task<StudentEnrolmentResponse> AssignStudentCohortAsync(Guid studentId, AssignStudentCohortRequest request, CancellationToken cancellationToken = default)
    {
        var enrolment = await _db.StudentEnrolments
            .FirstOrDefaultAsync(e => e.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("Student enrolment not found.");

        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(enrolment.UniversityId);

        var cohort = await _db.Cohorts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");

        if (cohort.UniversityId != enrolment.UniversityId)
            throw new ForbiddenException("Cohort must belong to the same university.");

        enrolment.CohortId = cohort.Id;
        enrolment.UpdatedAt = DateTime.UtcNow;

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StudentId == studentId, cancellationToken);
        if (user is not null)
        {
            user.CohortId = cohort.Id;
            await _userManager.UpdateAsync(user);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapStudentAsync(studentId, cancellationToken);
    }

    public async Task<CohortResponse> CreateCohortAsync(CreateCohortRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(request.UniversityId);

        var cohort = new Cohort
        {
            UniversityId = request.UniversityId,
            ProgrammeId = request.ProgrammeId,
            Name = request.Name,
            IntakeYear = request.IntakeYear,
            CurrentYear = request.CurrentYear,
            CurrentSemester = request.CurrentSemester
        };

        _db.Cohorts.Add(cohort);
        await _db.SaveChangesAsync(cancellationToken);

        return await MapCohortAsync(cohort.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CohortResponse>> GetCohortsAsync(CancellationToken cancellationToken = default)
    {
        var query = _db.Cohorts.AsNoTracking();
        if (!_tenant.IsApolloUser && _tenant.UniversityId.HasValue)
            query = query.Where(c => c.UniversityId == _tenant.UniversityId);

        var ids = await query.OrderByDescending(c => c.IntakeYear).Select(c => c.Id).ToListAsync(cancellationToken);
        var result = new List<CohortResponse>();
        foreach (var id in ids)
            result.Add(await MapCohortAsync(id, cancellationToken));
        return result;
    }

    private async Task<StudentEnrolmentResponse> MapStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        return await _db.StudentEnrolments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new StudentEnrolmentResponse(
                e.Id,
                e.StudentId,
                e.CohortId,
                _db.Users.Where(u => u.StudentId == studentId).Select(u => u.Email!).FirstOrDefault() ?? "",
                e.Student.FirstName,
                e.Student.LastName,
                e.Status.ToString(),
                e.Cohort.Name,
                e.Cohort.Programme.Name,
                e.CreatedAt,
                e.ActivatedAt))
            .FirstAsync(cancellationToken);
    }

    private async Task<CohortResponse> MapCohortAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Cohorts.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CohortResponse(c.Id, c.UniversityId, c.ProgrammeId, c.Name, c.IntakeYear, c.CurrentYear, c.CurrentSemester, c.Programme.Name))
            .FirstAsync(cancellationToken);
    }

    private sealed class CsvStudentRow
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Password { get; set; }
    }
}
