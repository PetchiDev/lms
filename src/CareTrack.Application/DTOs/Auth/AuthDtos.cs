namespace CareTrack.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    Guid? UniversityId,
    string? UniversityLogoUrl,
    Guid? CohortId,
    DateTime ExpiresAt);

public record ActivateAccountRequest(string Token, string Password, string ConfirmPassword);

public record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? UniversityId);

public record AuthUserResponse(
    string Id,
    string Email,
    string FullName,
    string Role,
    Guid? UniversityId,
    Guid? CohortId,
    Guid? StudentId);
