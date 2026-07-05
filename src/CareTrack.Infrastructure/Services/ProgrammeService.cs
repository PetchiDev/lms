using CareTrack.Application.Common;
using CareTrack.Application.DTOs.Programmes;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Entities;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class ProgrammeService : IProgrammeService
{
    private readonly CareTrackDbContext _db;
    private readonly ITenantContext _tenant;

    public ProgrammeService(CareTrackDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProgrammeResponse> CreateAsync(CreateProgrammeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can create programmes.");

        if (await _db.Programmes.AnyAsync(p => p.Code == request.Code, cancellationToken))
            throw new ConflictException("Programme code already exists.");

        var programme = new Programme
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            DurationYears = request.DurationYears
        };

        _db.Programmes.Add(programme);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProgrammeResponse(programme.Id, programme.Name, programme.Code, programme.Description, programme.DurationYears);
    }

    public async Task<IReadOnlyList<ProgrammeResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Programmes.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProgrammeResponse(p.Id, p.Name, p.Code, p.Description, p.DurationYears))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProgrammeStructureResponse> GetStructureAsync(Guid programmeId, CancellationToken cancellationToken = default)
    {
        var programme = await _db.Programmes.AsNoTracking()
            .Where(p => p.Id == programmeId)
            .Select(p => new ProgrammeStructureResponse(
                p.Id,
                p.Name,
                p.Years.OrderBy(y => y.YearNumber).Select(y => new ProgrammeYearResponse(
                    y.Id,
                    y.YearNumber,
                    y.Name,
                    y.Semesters.OrderBy(s => s.SemesterNumber).Select(s => new SemesterResponse(
                        s.Id,
                        s.SemesterNumber,
                        s.Name,
                        s.Modules.OrderBy(m => m.SortOrder).Select(m => new ModuleSummaryResponse(
                            m.Id, m.Title, m.Description, m.SortOrder)).ToList()
                    )).ToList()
                )).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Programme not found.");

        return programme;
    }

    public async Task<ProgrammeYearResponse> AddYearAsync(Guid programmeId, CreateProgrammeYearRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can modify programme structure.");

        _ = await _db.Programmes.FindAsync([programmeId], cancellationToken)
            ?? throw new NotFoundException("Programme not found.");

        var year = new ProgrammeYear
        {
            ProgrammeId = programmeId,
            YearNumber = request.YearNumber,
            Name = request.Name
        };

        _db.ProgrammeYears.Add(year);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProgrammeYearResponse(year.Id, year.YearNumber, year.Name, []);
    }

    public async Task<SemesterResponse> AddSemesterAsync(Guid yearId, CreateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can modify programme structure.");

        _ = await _db.ProgrammeYears.FindAsync([yearId], cancellationToken)
            ?? throw new NotFoundException("Programme year not found.");

        var semester = new Semester
        {
            ProgrammeYearId = yearId,
            SemesterNumber = request.SemesterNumber,
            Name = request.Name
        };

        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync(cancellationToken);

        return new SemesterResponse(semester.Id, semester.SemesterNumber, semester.Name, []);
    }

    public async Task<ModuleSummaryResponse> AddModuleAsync(Guid semesterId, CreateModuleRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenant.IsApolloUser)
            throw new ForbiddenException("Only Apollo users can modify programme structure.");

        _ = await _db.Semesters.FindAsync([semesterId], cancellationToken)
            ?? throw new NotFoundException("Semester not found.");

        var module = new Module
        {
            SemesterId = semesterId,
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder
        };

        _db.Modules.Add(module);
        await _db.SaveChangesAsync(cancellationToken);

        return new ModuleSummaryResponse(module.Id, module.Title, module.Description, module.SortOrder);
    }
}
