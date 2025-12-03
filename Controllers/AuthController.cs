using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projekt.Auth;
using Projekt.Dtos;
using Projekt.Infrastructure.Data;
using Projekt.Infrastructure.Entities;

namespace Projekt.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { error = "User with this email already exists" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName,
        };

        _context.Users.Add(user);

        // Assign default "user" role
        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "user");

        if (userRole != null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id
            });
        }

        await _context.SaveChangesAsync();

        var response = new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        };

        return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
    }

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Find user
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        // Get user roles
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user, roles);

        var response = new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = 3600, // 60 minutes in seconds
            TokenType = "Bearer"
        };

        return Ok(response);
    }

    /// <summary>
    /// Grant admin privileges to a user (Admin only)
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpPost("grant-admin")]
    public async Task<IActionResult> GrantAdmin([FromBody] GrantAdminRequest request)
    {
        // Find user by email
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Check if user already has admin role
        var hasAdminRole = user.UserRoles.Any(ur => ur.Role.Name == "admin");
        if (hasAdminRole)
        {
            return Conflict(new { error = "User already has admin privileges" });
        }

        // Get admin role
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
        if (adminRole == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Admin role not configured in the system" });
        }

        // Add admin role to user
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        await _context.SaveChangesAsync();

        // Get updated roles list
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        var response = new GrantAdminResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles
        };

        return Ok(response);
    }
}