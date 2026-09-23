using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gamestore.BLL.Constants;
using Gamestore.BLL.Services.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Gamestore.BLL.Services.Concrete;

public class JwtTokenService : IJwtTokenService
{
    private const int MinimumSigningKeySizeInBytes = 32;

    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly string _issuer;
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IConfiguration configuration, TimeProvider timeProvider)
    {
        _issuer = GetRequiredSetting(configuration, "Jwt:Issuer");
        _audience = GetRequiredSetting(configuration, "Jwt:Audience");
        var signingKey = GetRequiredSetting(configuration, "Jwt:Key");

        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeySizeInBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {MinimumSigningKeySizeInBytes} bytes long.");
        }

        if (!int.TryParse(configuration["Jwt:ExpirationMinutes"], out _expirationMinutes)
            || _expirationMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:ExpirationMinutes must be a positive integer.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
        _timeProvider = timeProvider;
    }

    public string GenerateToken(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtClaimNames.UniqueName, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => new Claim(JwtClaimNames.Role, userRole.Role.Name)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_expirationMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetRequiredSetting(IConfiguration configuration, string key)
    {
        return configuration[key]
            ?? throw new InvalidOperationException($"{key} configuration is required.");
    }
}
