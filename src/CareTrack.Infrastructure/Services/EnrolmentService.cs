using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Enrolment;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using ClosedXML.Excel;
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
                ActivatedAt = now,
                CurrentYear = 1,
                CurrentSemester = 1
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

    public async Task<CsvImportResult> ImportStudentsAsync(
        Stream fileStream,
        string fileName,
        Guid cohortId,
        CancellationToken cancellationToken = default)
    {
        var cohort = await _db.Cohorts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cohortId, cancellationToken)
            ?? throw new NotFoundException("Cohort not found.");

        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(cohort.UniversityId);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var rows = extension switch
        {
            ".csv" => await ParseCsvRowsAsync(fileStream, cancellationToken),
            ".xlsx" => ParseXlsxRows(fileStream),
            _ => throw new Domain.Exceptions.ValidationException("Only CSV and XLSX files are supported.")
        };

        if (rows.Count == 0)
            throw new Domain.Exceptions.ValidationException("No student rows found in the file.");

        var errors = new List<string>();
        var success = 0;
        var total = 0;

        foreach (var record in rows)
        {
            total++;
            try
            {
                if (string.IsNullOrWhiteSpace(record.Email))
                    throw new Domain.Exceptions.ValidationException("Email is required.");

                await CreateStudentAsync(
                    new CreateStudentRequest(
                        record.Email.Trim(),
                        record.FirstName.Trim(),
                        record.LastName.Trim(),
                        cohortId,
                        string.IsNullOrWhiteSpace(record.Password) ? "Student@123" : record.Password.Trim()),
                    cancellationToken);
                success++;
            }
            catch (Exception ex)
            {
                var label = string.IsNullOrWhiteSpace(record.Email) ? $"Row {total}" : record.Email;
                errors.Add($"Row {total} ({label}): {ex.Message}");
            }
        }

        return new CsvImportResult(total, success, total - success, errors);
    }

    private static async Task<List<ImportStudentRow>> ParseCsvRowsAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var rows = new List<ImportStudentRow>();
        await foreach (var record in csv.GetRecordsAsync<ImportStudentRow>(cancellationToken))
            rows.Add(record);
        return rows;
    }

    private static List<ImportStudentRow> ParseXlsxRows(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;

        using var workbook = new XLWorkbook(memory);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.RowsUsed().Any())
            ?? throw new Domain.Exceptions.ValidationException("Worksheet is empty.");

        var headerRow = worksheet.FirstRowUsed()
            ?? throw new Domain.Exceptions.ValidationException("Worksheet has no header row.");

        var emailCol = FindColumn(headerRow, "email");
        var firstNameCol = FindColumn(headerRow, "firstname", "first name", "first_name");
        var lastNameCol = FindColumn(headerRow, "lastname", "last name", "last_name");
        var passwordCol = FindColumn(headerRow, "password");

        if (emailCol is null || firstNameCol is null || lastNameCol is null)
            throw new Domain.Exceptions.ValidationException("XLSX must include Email, FirstName, and LastName columns.");

        var rows = new List<ImportStudentRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

        for (var rowNum = headerRow.RowNumber() + 1; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty())
                continue;

            var email = row.Cell(emailCol.Value).GetString().Trim();
            var firstName = row.Cell(firstNameCol.Value).GetString().Trim();
            var lastName = row.Cell(lastNameCol.Value).GetString().Trim();
            var password = passwordCol.HasValue ? row.Cell(passwordCol.Value).GetString().Trim() : null;

            if (string.IsNullOrWhiteSpace(email) &&
                string.IsNullOrWhiteSpace(firstName) &&
                string.IsNullOrWhiteSpace(lastName))
                continue;

            rows.Add(new ImportStudentRow
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Password = string.IsNullOrWhiteSpace(password) ? null : password
            });
        }

        return rows;
    }

    private static int? FindColumn(IXLRow headerRow, params string[] names)
    {
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeHeader(cell.GetString());
            if (names.Any(n => header == NormalizeHeader(n)))
                return cell.Address.ColumnNumber;
        }

        return null;
    }

    private static string NormalizeHeader(string value) =>
        value.Trim().Replace(" ", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .ToLowerInvariant();

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

    public async Task<StudentEnrolmentResponse> UpdateStudentAsync(
        Guid studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var enrolment = await _db.StudentEnrolments
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("Student enrolment not found.");

        if (!_tenant.IsApolloUser)
            _tenant.EnsureUniversityAccess(enrolment.UniversityId);

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new Domain.Exceptions.ValidationException("First name and last name are required.");

        enrolment.Student.FirstName = request.FirstName.Trim();
        enrolment.Student.LastName = request.LastName.Trim();
        enrolment.Student.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<EnrolmentStatus>(request.Status, true, out var status))
                throw new Domain.Exceptions.ValidationException("Invalid status. Use Active, Invited, or Suspended.");
            enrolment.Status = status;
        }

        if (request.CohortId.HasValue && request.CohortId.Value != enrolment.CohortId)
        {
            var cohort = await _db.Cohorts.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CohortId.Value, cancellationToken)
                ?? throw new NotFoundException("Cohort not found.");

            if (cohort.UniversityId != enrolment.UniversityId)
                throw new ForbiddenException("Cohort must belong to the same university.");

            enrolment.CohortId = cohort.Id;
        }

        enrolment.UpdatedAt = DateTime.UtcNow;

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.StudentId == studentId, cancellationToken);
        if (user is not null)
        {
            user.FirstName = enrolment.Student.FirstName;
            user.LastName = enrolment.Student.LastName;

            if (request.CohortId.HasValue)
                user.CohortId = enrolment.CohortId;

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<EnrolmentStatus>(request.Status, true, out var userStatus))
                user.Status = userStatus;

            if (!string.IsNullOrWhiteSpace(request.Email) &&
                !string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
                if (existing is not null && existing.Id != user.Id)
                    throw new ConflictException("User with this email already exists.");

                user.Email = request.Email.Trim();
                user.UserName = request.Email.Trim();
                user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
                user.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new Domain.Exceptions.ValidationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passwordResult.Succeeded)
                    throw new Domain.Exceptions.ValidationException(string.Join("; ", passwordResult.Errors.Select(e => e.Description)));
            }
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

    private sealed class ImportStudentRow
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Password { get; set; }
    }
}
