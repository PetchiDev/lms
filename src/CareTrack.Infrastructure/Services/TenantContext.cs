using CareTrack.Application;
using CareTrack.Application.Common;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CareTrack.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public UserRole Role
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");
            return Enum.TryParse<UserRole>(role, out var parsed) ? parsed : UserRole.Student;
        }
    }

    public Guid? UniversityId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("universityId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? CohortId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("cohortId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? StudentId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("studentId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? SupervisorId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("supervisorId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsApolloUser => Role is UserRole.ApolloAdmin or UserRole.ApolloFaculty;

    public void EnsureUniversityAccess(Guid universityId)
    {
        if (IsApolloUser) return;
        if (UniversityId != universityId)
            throw new ForbiddenException("Access denied for this university.");
    }
}
