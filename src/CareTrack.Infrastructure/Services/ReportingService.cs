using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Reports;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;

    public ReportingService(CareTrackDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<CohortReportResponse> GetUniversityStudentReportAsync(Guid? cohortId, Guid? universityId, CancellationToken cancellationToken = default)
    {
        if (_tenant.Role != UserRole.UniversityAdmin && !_tenant.IsApolloUser)
            throw new ForbiddenException("Not authorized.");

        var query = _db.StudentEnrolments.AsNoTracking()
            .Where(e => e.Status == EnrolmentStatus.Active);

        if (!_tenant.IsApolloUser && _tenant.UniversityId.HasValue)
            query = query.Where(e => e.UniversityId == _tenant.UniversityId);
        else if (_tenant.IsApolloUser && universityId.HasValue)
            query = query.Where(e => e.UniversityId == universityId);

        if (cohortId.HasValue)
            query = query.Where(e => e.CohortId == cohortId);

        var enrolments = await query
            .Select(e => new
            {
                e.StudentId,
                e.Student.FirstName,
                e.Student.LastName,
                Email = _db.Users.Where(u => u.StudentId == e.StudentId).Select(u => u.Email).FirstOrDefault(),
                e.Cohort.Name,
                LastActivity = _db.LessonProgresses.Where(p => p.StudentId == e.StudentId).Max(p => (DateTime?)p.LastActivityAt),
                Progress = _db.ModuleProgresses.Where(p => p.StudentId == e.StudentId).Average(p => (double?)p.ProgressPercent) ?? 0
            })
            .ToListAsync(cancellationToken);

        var students = enrolments.Select(e =>
        {
            var progress = (int)e.Progress;
            var atRisk = progress < 40 || e.LastActivity < DateTime.UtcNow.AddDays(-7);
            return new StudentProgressReport(
                e.StudentId,
                $"{e.FirstName} {e.LastName}",
                e.Email ?? "",
                e.Name,
                progress,
                atRisk,
                e.LastActivity);
        }).ToList();

        var cohortName = cohortId.HasValue
            ? await _db.Cohorts.Where(c => c.Id == cohortId).Select(c => c.Name).FirstAsync(cancellationToken)
            : "All Cohorts";

        return new CohortReportResponse(
            cohortName,
            students.Count,
            students.Count,
            students.Count(s => s.IsAtRisk),
            students.Count == 0 ? 0 : students.Average(s => s.ProgressPercent),
            students);
    }

    public async Task<IReadOnlyList<UniversityComparisonReport>> GetApolloUniversityReportAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Apollo access only.");

        return await _db.Universities.AsNoTracking()
            .Select(u => new UniversityComparisonReport(
                u.Id,
                u.Name,
                u.Enrolments.Count(e => e.Status == EnrolmentStatus.Active),
                u.Enrolments.Where(e => e.Status == EnrolmentStatus.Active)
                    .Select(e => _db.ModuleProgresses.Where(p => p.StudentId == e.StudentId).Average(p => (double?)p.ProgressPercent) ?? 0)
                    .Average(),
                u.Enrolments.Count(e => e.Status == EnrolmentStatus.Active &&
                    (_db.ModuleProgresses.Where(p => p.StudentId == e.StudentId).Average(p => (double?)p.ProgressPercent) ?? 0) < 40)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentPerformanceReport>> GetContentPerformanceAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Apollo access only.");

        return await _db.Modules.AsNoTracking()
            .Select(m => new ContentPerformanceReport(
                m.Id,
                m.Title,
                m.Semester.ProgrammeYear.Programme.Name,
                _db.StudentEnrolments.Count(e => e.Cohort.ProgrammeId == m.Semester.ProgrammeYear.ProgrammeId),
                _db.ModuleProgresses.Count(p => p.ModuleId == m.Id && p.IsCompleted),
                _db.StudentEnrolments.Count(e => e.Cohort.ProgrammeId == m.Semester.ProgrammeYear.ProgrammeId) == 0
                    ? 0
                    : _db.ModuleProgresses.Count(p => p.ModuleId == m.Id && p.IsCompleted) * 100.0 /
                      _db.StudentEnrolments.Count(e => e.Cohort.ProgrammeId == m.Semester.ProgrammeYear.ProgrammeId)))
            .OrderByDescending(r => r.EnrolledStudents)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<byte[]> ExportReportAsync(ExportReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await GetUniversityStudentReportAsync(request.CohortId, null, cancellationToken);

        if (request.Format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            var lines = report.Students.Select(s =>
                $"{s.FullName},{s.Email},{s.CohortName},{s.ProgressPercent},{s.IsAtRisk}");
            return System.Text.Encoding.UTF8.GetBytes(string.Join('\n', lines));
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Student Progress");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Cohort";
        ws.Cell(1, 4).Value = "Progress %";
        ws.Cell(1, 5).Value = "At Risk";

        var row = 2;
        foreach (var s in report.Students)
        {
            ws.Cell(row, 1).Value = s.FullName;
            ws.Cell(row, 2).Value = s.Email;
            ws.Cell(row, 3).Value = s.CohortName;
            ws.Cell(row, 4).Value = s.ProgressPercent;
            ws.Cell(row, 5).Value = s.IsAtRisk ? "Yes" : "No";
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
