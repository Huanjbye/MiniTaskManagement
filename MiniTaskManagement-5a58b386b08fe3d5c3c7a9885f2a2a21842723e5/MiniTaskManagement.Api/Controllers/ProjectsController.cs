using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniTaskManagement.Api.Data;
using MiniTaskManagement.Api.DTOs;
using MiniTaskManagement.Api.Entities;

namespace MiniTaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ProjectsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProjects()
    {
        var userId = CurrentUserId();

        var projects = await _dbContext.Projects
            .Where(x => x.OwnerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.Status,
                x.StartDate,
                x.DueDate,
                x.CreatedAt,
                x.UpdatedAt,
                TaskCount = x.Tasks.Count,
                DoneTaskCount = x.Tasks.Count(task => task.Status == "Done"),
                Progress = x.Tasks.Count == 0
                    ? 0
                    : (int)Math.Round(
                        x.Tasks.Count(task => task.Status == "Done") * 100.0 /
                        x.Tasks.Count)
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        var userId = CurrentUserId();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.Name.Trim(),
            Description = request.Description,
            StartDate = ToUtc(request.StartDate),
            DueDate = ToUtc(request.DueDate),
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        return Ok(project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required");

        var userId = CurrentUserId();

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OwnerId == userId);

        if (project == null)
            return NotFound();

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.Status = request.Status;
        project.StartDate = ToUtc(request.StartDate);
        project.DueDate = ToUtc(request.DueDate);
        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(project);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId();

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OwnerId == userId);

        if (project == null)
            return NotFound();

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();

        return Ok("Deleted successfully");
    }

    private Guid CurrentUserId()
    {
        return Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
    }
}
