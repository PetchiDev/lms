using CareTrack.Application.DTOs.Auth;
using CareTrack.Application.Interfaces;
using CareTrack.Domain.Enums;
using CareTrack.Domain.Exceptions;
using CareTrack.Infrastructure;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly CareTrackDbContext _db;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings,
        CareTrackDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
        _db = db;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException("Invalid email or password.");

        if (user.Status == EnrolmentStatus.Invited)
            throw new ValidationException("Account not activated. Please check your invite email.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            throw new NotFoundException("Invalid email or password.");

        if (user.Role == UserRole.Student && !user.StudentId.HasValue)
        {
            var studentId = await _db.Students.AsNoTracking()
                .Where(s => s.UserId == user.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId != Guid.Empty)
            {
                user.StudentId = studentId;
                await _userManager.UpdateAsync(user);
            }
        }

        var token = _jwtTokenService.GenerateToken(
            user.Id, user.Email!, user.Role.ToString(), user.UniversityId, user.CohortId, user.StudentId, user.SupervisorId);

        return new LoginResponse(
            token,
            user.Email!,
            user.FullName,
            user.Role.ToString(),
            user.UniversityId,
            user.CohortId,
            DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));
    }

    public async Task ActivateAccountAsync(ActivateAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
            throw new ValidationException("Passwords do not match.");

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.InviteToken == request.Token && u.InviteTokenExpiry > DateTime.UtcNow, cancellationToken)
            ?? throw new NotFoundException("Invalid or expired activation token.");

        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.Status = EnrolmentStatus.Active;
        user.EmailConfirmed = true;

        await _userManager.UpdateAsync(user);
    }

    public async Task InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ValidationException("Invalid role.");

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            throw new ConflictException("User with this email already exists.");

        var token = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = role,
            UniversityId = request.UniversityId,
            Status = EnrolmentStatus.Invited,
            InviteToken = token,
            InviteTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        await _userManager.CreateAsync(user);
        // Email sent by caller or via IEmailService injection if needed
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return null; // Populated via controller from claims
    }
}
