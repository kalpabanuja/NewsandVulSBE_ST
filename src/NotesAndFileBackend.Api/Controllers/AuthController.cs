using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Api.DTOs;
using NotesAndFileBackend.Core.Entities;
using NotesAndFileBackend.Core.Interfaces;
using NotesAndFileBackend.Infrastructure.Data;

namespace NotesAndFileBackend.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    private string HashPassword(string password)
    {
        if (password == null) return string.Empty;
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    [HttpPost("sign-up")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Conflict(new { error = new { code = "EMAIL_IN_USE", message = "Email already in use." } });
        }

        var user = new User
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = HashPassword(request.Password),
            Status = "ACTIVE"
        };

        var device = new Device
        {
            User = user,
            DeviceName = request.DeviceName,
            Platform = request.Platform,
            AppVersion = "1.0.0" // Ideally read from headers
        };

        _context.Users.Add(user);
        _context.Devices.Add(device);
        
        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, device);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            DeviceId = device.Id
        });
    }

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        _logger.LogInformation($"SignIn attempt for email: '{request.Email}'");
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning($"SignIn failed: User with email '{request.Email}' not found in database.");
            return Unauthorized(new { error = new { code = "INVALID_CREDENTIALS", message = "Invalid email or password." } });
        }

        var providedHash = HashPassword(request.Password);
        if (user.PasswordHash != providedHash)
        {
            _logger.LogWarning($"SignIn failed for '{request.Email}': Password hash mismatch. Expected: {user.PasswordHash}, Got: {providedHash}. Password length provided: {request.Password?.Length}");
            return Unauthorized(new { error = new { code = "INVALID_CREDENTIALS", message = "Invalid email or password." } });
        }

        var device = await _context.Devices.FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceName == request.DeviceName);
        if (device == null)
        {
            device = new Device
            {
                UserId = user.Id,
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                AppVersion = "1.0.0"
            };
            _context.Devices.Add(device);
        }
        else
        {
            device.LastSeenAt = DateTime.UtcNow;
            _context.Devices.Update(device);
        }

        user.LastLoginAt = DateTime.UtcNow;
        _context.Users.Update(user);

        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, device);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            DeviceId = device.Id
        });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            return Unauthorized();
            
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();
            
        bool changed = false;
        
        if (!string.IsNullOrWhiteSpace(request.DisplayName) && user.DisplayName != request.DisplayName)
        {
            user.DisplayName = request.DisplayName;
            changed = true;
        }
        
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = HashPassword(request.Password);
            changed = true;
        }
        
        if (changed)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        
        return Ok();
    }
}
