using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NotesAndFileBackend.Domain.Entities;
using NotesAndFileBackend.Application.Interfaces;

namespace NotesAndFileBackend.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(User user, Device device)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("deviceId", device.Id.ToString())
        };

        var secret = _config["JwtSettings:Secret"] ?? "fallback_secret_key_that_is_at_least_32_bytes_long_12345!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expirationMinutesString = _config["JwtSettings:ExpirationInMinutes"];
        var expirationMinutes = double.TryParse(expirationMinutesString, out var parsed) ? parsed : 60.0;
        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"] ?? "NotesAndFileBackend",
            audience: _config["JwtSettings:Audience"] ?? "NotesAndFileBackend.Users",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Simple random string for refresh token. For production, consider using a cryptographically secure generator.
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
