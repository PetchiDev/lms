using CareTrack.Application.Interfaces;
using CareTrack.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CareTrack.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(string userId, string email, string role, Guid? universityId, Guid? cohortId, Guid? studentId, Guid? supervisorId = null)
    {
        var claims = new List<Claim>
        {
            new("sub", userId),
            new("email", email),
            new("role", role)
        };

        if (universityId.HasValue)
            claims.Add(new Claim("universityId", universityId.Value.ToString()));
        if (cohortId.HasValue)
            claims.Add(new Claim("cohortId", cohortId.Value.ToString()));
        if (studentId.HasValue)
            claims.Add(new Claim("studentId", studentId.Value.ToString()));
        if (supervisorId.HasValue)
            claims.Add(new Claim("supervisorId", supervisorId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
