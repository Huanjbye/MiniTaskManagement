using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniTaskManagement.Api.Data;
using MiniTaskManagement.Api.DTOs;
using MiniTaskManagement.Api.Entities;
using MiniTaskManagement.Api.Services;

namespace MiniTaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly JwtService _jwtService;

    public AuthController(
        AppDbContext dbContext,
        JwtService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var emailExists = await _dbContext.Users
            .AnyAsync(x => x.Email == request.Email);

        if (emailExists)
        {
            return BadRequest("Email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync();

        return Ok("Register successful");
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
        {
            return Unauthorized("Invalid email or password");
        }
        if (!user.IsActive)
        {
            return Unauthorized("Account has been disabled");
        }

        var validPassword =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!validPassword)
        {
            return Unauthorized("Invalid email or password");
        }

        var token = _jwtService.GenerateToken(user);

return Ok(new AuthResponse
{
    Token = token,
    Role = user.Role,
    FullName = user.FullName,
    Email = user.Email
});
    }
}
