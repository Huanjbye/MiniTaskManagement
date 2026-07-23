using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniTaskManagement.Api.Data;
using MiniTaskManagement.Api.DTOs;
using System.Security.Claims;

namespace MiniTaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {

        var users = await _dbContext.Users
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("users/{id}/disable")]
    public async Task<IActionResult> DisableUser(Guid id)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound();

        user.IsActive = false;

        await _dbContext.SaveChangesAsync();

        return Ok(user);
    }
    [HttpPut("users/{id}/enable")]
    public async Task<IActionResult> EnableUser(Guid id)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound();

        user.IsActive = true;

        await _dbContext.SaveChangesAsync();

        return Ok(user);
    }
    [HttpGet("tasks")]
public async Task<IActionResult> GetAllTasks()
{
    var tasks = await _dbContext.Tasks
        .Include(t => t.User)
        .Select(t => new
        {
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.DueDate,
            t.CreatedAt,
            UserEmail = t.User.Email,
            UserName = t.User.FullName
        })
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync();

    return Ok(tasks);
}
    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (currentUserId == id.ToString())
        {
            return BadRequest("You cannot change your own role.");
        }
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user == null)
            return NotFound();

        if (request.Role != "Admin" && request.Role != "User")
            return BadRequest("Invalid role");

        user.Role = request.Role;

        await _dbContext.SaveChangesAsync();

        return Ok(user);
    }




}


