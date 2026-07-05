using CareTrack.Application.DTOs.Auth;

namespace CareTrack.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task ActivateAccountAsync(ActivateAccountRequest request, CancellationToken cancellationToken = default);
    Task InviteUserAsync(InviteUserRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
