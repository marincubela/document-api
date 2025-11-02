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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { error = "User with this email already exists" });
        }

        try
        {
            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while registering the user" });
        }
    }

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
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

            // Check if user is active
            if (!user.IsActive)
            {
                return Unauthorized(new { error = "User account is inactive" });
            }

            // Get user roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            // Generate JWT token
            var token = _jwtTokenService.GenerateToken(user, roles);
            var expirationMinutes = int.Parse(_context.Database.GetDbConnection().ConnectionString.Contains("Jwt:ExpirationMinutes") 
                ? "60" 
                : "60");

            var response = new LoginResponse
            {
                AccessToken = token,
                ExpiresIn = 3600, // 60 minutes in seconds
                TokenType = "Bearer"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred during login" });
        }
    }
}

