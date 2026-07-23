using System.Security.Claims;
using System.Text;
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
public class TasksController : ControllerBase
{
    private static readonly string[] AllowedStatuses =
        ["Todo", "InProgress", "Review", "Done"];

    private static readonly string[] AllowedPriorities =
        ["Low", "Medium", "High", "Critical"];

    private readonly AppDbContext _dbContext;

    public TasksController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required");

        if (!AllowedPriorities.Contains(request.Priority))
            return BadRequest("Invalid priority");

        if (request.DueDate.Date < DateTime.UtcNow.Date)
            return BadRequest("DueDate cannot be earlier than today");

        var userId = CurrentUserId();

        if (request.ProjectId.HasValue &&
            !await OwnsProject(userId, request.ProjectId.Value))
        {
            return BadRequest("Project is invalid");
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            Status = "Todo",
            DueDate = ToUtc(request.DueDate),
            ReminderAt = ToUtc(request.ReminderAt),
            EstimatedMinutes = request.EstimatedMinutes,
            ActualMinutes = Math.Max(0, request.ActualMinutes),
            ProjectId = request.ProjectId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        await SyncTags(task, userId, request.TagNames);
        AddActivity(task.Id, userId, "Created task", null);

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] DateTime? dueDate,
        [FromQuery] string? dueStatus,
        [FromQuery] Guid? projectId,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5)
    {
        var userId = CurrentUserId();

        var query = _dbContext.Tasks
            .Where(x => x.UserId == userId)
            .AsQueryable();

        query = ApplyTaskFilters(
            query,
            search,
            status,
            priority,
            dueDate,
            dueStatus,
            projectId,
            tag);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var totalCount = await query.CountAsync();

        var tasks = await query
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.ReminderAt,
                x.EstimatedMinutes,
                x.ActualMinutes,
                x.ProjectId,
                ProjectName = x.Project == null ? null : x.Project.Name,
                x.CreatedAt,
                x.UpdatedAt,
                TagNames = x.TaskTags
                    .Select(taskTag => taskTag.Tag.Name)
                    .OrderBy(name => name)
                    .ToList(),
                SubtaskTotal = x.Subtasks.Count,
                SubtaskDone = x.Subtasks.Count(subtask => subtask.IsDone)
            })
            .ToListAsync();

        return Ok(new
        {
            Items = tasks,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = CurrentUserId();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(7);

        var query = _dbContext.Tasks
            .Where(x => x.UserId == userId);

        var stats = new
        {
            TotalTasks = await query.CountAsync(),
            TodoTasks = await query.CountAsync(x => x.Status == "Todo"),
            InProgressTasks = await query.CountAsync(x => x.Status == "InProgress"),
            ReviewTasks = await query.CountAsync(x => x.Status == "Review"),
            DoneTasks = await query.CountAsync(x => x.Status == "Done"),
            DueTodayTasks = await query.CountAsync(x =>
                x.DueDate >= today &&
                x.DueDate < tomorrow &&
                x.Status != "Done"),
            ThisWeekTasks = await query.CountAsync(x =>
                x.DueDate >= today &&
                x.DueDate < nextWeek &&
                x.Status != "Done"),
            OverdueTasks = await query.CountAsync(x =>
                x.DueDate < today &&
                x.Status != "Done"),
            CriticalTasks = await query.CountAsync(x => x.Priority == "Critical")
        };

        return Ok(stats);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        var userId = CurrentUserId();

        var tags = await _dbContext.Tags
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Color
            })
            .ToListAsync();

        return Ok(tags);
    }

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var userId = CurrentUserId();

        var tasks = await _dbContext.Tasks
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.EstimatedMinutes,
                x.ActualMinutes,
                ProjectName = x.Project == null ? "" : x.Project.Name
            })
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Title,Description,Status,Priority,DueDate,EstimatedMinutes,ActualMinutes,Project");

        foreach (var task in tasks)
        {
            csv.AppendLine(string.Join(",", [
                Csv(task.Title),
                Csv(task.Description ?? ""),
                Csv(task.Status),
                Csv(task.Priority),
                Csv(task.DueDate.ToString("yyyy-MM-dd")),
                Csv(task.EstimatedMinutes?.ToString() ?? ""),
                Csv(task.ActualMinutes.ToString()),
                Csv(task.ProjectName)
            ]));
        }

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            "tasks.csv");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = CurrentUserId();
        var task = await BuildTaskDetails(id, userId);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId();

        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        _dbContext.Tasks.Remove(task);

        await _dbContext.SaveChangesAsync();

        return Ok("Deleted successfully");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required");

        if (!AllowedStatuses.Contains(request.Status))
            return BadRequest("Invalid status");

        if (!AllowedPriorities.Contains(request.Priority))
            return BadRequest("Invalid priority");

        if (request.DueDate.Date < DateTime.UtcNow.Date)
            return BadRequest("DueDate cannot be earlier than today");

        var userId = CurrentUserId();

        if (request.ProjectId.HasValue &&
            !await OwnsProject(userId, request.ProjectId.Value))
        {
            return BadRequest("Project is invalid");
        }

        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        var oldStatus = task.Status;

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = ToUtc(request.DueDate);
        task.ReminderAt = ToUtc(request.ReminderAt);
        task.EstimatedMinutes = request.EstimatedMinutes;
        task.ActualMinutes = Math.Max(0, request.ActualMinutes);
        task.ProjectId = request.ProjectId;
        task.UpdatedAt = DateTime.UtcNow;

        await SyncTags(task, userId, request.TagNames);

        AddActivity(
            task.Id,
            userId,
            "Updated task",
            oldStatus == task.Status
                ? null
                : $"Status changed from {oldStatus} to {task.Status}");

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        UpdateTaskStatusRequest request)
    {
        if (!AllowedStatuses.Contains(request.Status))
            return BadRequest("Invalid status");

        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        var oldStatus = task.Status;
        task.Status = request.Status;
        task.SortOrder = request.SortOrder;
        task.UpdatedAt = DateTime.UtcNow;

        AddActivity(
            task.Id,
            userId,
            "Changed status",
            $"Status changed from {oldStatus} to {task.Status}");

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPost("{id}/subtasks")]
    public async Task<IActionResult> CreateSubtask(
        Guid id,
        UpsertSubtaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required");

        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        var subtask = new TaskSubtask
        {
            Id = Guid.NewGuid(),
            TaskItemId = task.Id,
            Title = request.Title.Trim(),
            IsDone = request.IsDone,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = request.IsDone ? DateTime.UtcNow : null
        };

        _dbContext.TaskSubtasks.Add(subtask);
        AddActivity(task.Id, userId, "Added checklist item", subtask.Title);

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPut("{id}/subtasks/{subtaskId}")]
    public async Task<IActionResult> UpdateSubtask(
        Guid id,
        Guid subtaskId,
        UpsertSubtaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required");

        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        var subtask = await _dbContext.TaskSubtasks
            .FirstOrDefaultAsync(x =>
                x.Id == subtaskId &&
                x.TaskItemId == id);

        if (subtask == null)
            return NotFound();

        var wasDone = subtask.IsDone;

        subtask.Title = request.Title.Trim();
        subtask.IsDone = request.IsDone;
        subtask.SortOrder = request.SortOrder;
        subtask.CompletedAt = request.IsDone
            ? subtask.CompletedAt ?? DateTime.UtcNow
            : null;

        if (wasDone != subtask.IsDone)
        {
            AddActivity(
                task.Id,
                userId,
                subtask.IsDone ? "Completed checklist item" : "Reopened checklist item",
                subtask.Title);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpDelete("{id}/subtasks/{subtaskId}")]
    public async Task<IActionResult> DeleteSubtask(
        Guid id,
        Guid subtaskId)
    {
        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        var subtask = await _dbContext.TaskSubtasks
            .FirstOrDefaultAsync(x =>
                x.Id == subtaskId &&
                x.TaskItemId == id);

        if (subtask == null)
            return NotFound();

        _dbContext.TaskSubtasks.Remove(subtask);
        AddActivity(task.Id, userId, "Deleted checklist item", subtask.Title);

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id,
        CreateCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Content is required");

        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        _dbContext.TaskComments.Add(new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskItemId = task.Id,
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        AddActivity(task.Id, userId, "Added comment", null);

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPut("{id}/tags")]
    public async Task<IActionResult> SetTags(
        Guid id,
        SetTaskTagsRequest request)
    {
        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        await SyncTags(task, userId, request.TagNames);
        AddActivity(task.Id, userId, "Updated tags", string.Join(", ", request.TagNames));

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPost("{id}/time/start")]
    public async Task<IActionResult> StartTimer(Guid id)
    {
        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        if (task.TimerStartedAt.HasValue)
            return BadRequest("Timer is already running");

        task.TimerStartedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        AddActivity(task.Id, userId, "Started timer", null);

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPost("{id}/time/stop")]
    public async Task<IActionResult> StopTimer(Guid id)
    {
        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        if (!task.TimerStartedAt.HasValue)
            return BadRequest("Timer is not running");

        var elapsed = DateTime.UtcNow - task.TimerStartedAt.Value;
        var minutes = Math.Max(1, (int)Math.Round(elapsed.TotalMinutes));

        task.ActualMinutes += minutes;
        task.TimerStartedAt = null;
        task.UpdatedAt = DateTime.UtcNow;

        AddActivity(task.Id, userId, "Stopped timer", $"{minutes} minutes");

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    [HttpPost("{id}/time/add")]
    public async Task<IActionResult> AddActualTime(
        Guid id,
        AddActualTimeRequest request)
    {
        if (request.Minutes <= 0)
            return BadRequest("Minutes must be greater than zero");

        var userId = CurrentUserId();
        var task = await FindOwnedTask(id, userId);

        if (task == null)
            return NotFound();

        task.ActualMinutes += request.Minutes;
        task.UpdatedAt = DateTime.UtcNow;

        AddActivity(task.Id, userId, "Added actual time", $"{request.Minutes} minutes");

        await _dbContext.SaveChangesAsync();

        return Ok(await BuildTaskDetails(task.Id, userId));
    }

    private IQueryable<TaskItem> ApplyTaskFilters(
        IQueryable<TaskItem> query,
        string? search,
        string? status,
        string? priority,
        DateTime? dueDate,
        string? dueStatus,
        Guid? projectId,
        string? tag)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();

            query = query.Where(x =>
                x.Title.ToLower().Contains(keyword) ||
                (x.Description != null &&
                    x.Description.ToLower().Contains(keyword)) ||
                x.TaskTags.Any(taskTag =>
                    taskTag.Tag.Name.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x =>
                x.Status.ToLower() == status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x =>
                x.Priority.ToLower() == priority.ToLower());
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagName = tag.Trim().ToLower();

            query = query.Where(x =>
                x.TaskTags.Any(taskTag =>
                    taskTag.Tag.Name.ToLower() == tagName));
        }

        if (dueDate.HasValue)
        {
            var date = DateTime.SpecifyKind(
                dueDate.Value.Date,
                DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            query = query.Where(x =>
                x.DueDate >= date &&
                x.DueDate < nextDate);
        }

        if (!string.IsNullOrWhiteSpace(dueStatus))
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            switch (dueStatus.ToLower())
            {
                case "today":
                    query = query.Where(x =>
                        x.DueDate >= today &&
                        x.DueDate < tomorrow &&
                        x.Status != "Done");
                    break;

                case "overdue":
                    query = query.Where(x =>
                        x.DueDate < today &&
                        x.Status != "Done");
                    break;

                case "thisweek":
                    var endOfWeek = today.AddDays(7);

                    query = query.Where(x =>
                        x.DueDate >= today &&
                        x.DueDate < endOfWeek &&
                        x.Status != "Done");
                    break;
            }
        }

        return query;
    }

    private async Task<object?> BuildTaskDetails(Guid id, Guid userId)
    {
        return await _dbContext.Tasks
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.ReminderAt,
                x.EstimatedMinutes,
                x.ActualMinutes,
                x.TimerStartedAt,
                x.ProjectId,
                ProjectName = x.Project == null ? null : x.Project.Name,
                x.CreatedAt,
                x.UpdatedAt,
                Tags = x.TaskTags
                    .Select(taskTag => new
                    {
                        taskTag.Tag.Id,
                        taskTag.Tag.Name,
                        taskTag.Tag.Color
                    })
                    .OrderBy(tag => tag.Name)
                    .ToList(),
                TagNames = x.TaskTags
                    .Select(taskTag => taskTag.Tag.Name)
                    .OrderBy(name => name)
                    .ToList(),
                Subtasks = x.Subtasks
                    .OrderBy(subtask => subtask.SortOrder)
                    .ThenBy(subtask => subtask.CreatedAt)
                    .Select(subtask => new
                    {
                        subtask.Id,
                        subtask.Title,
                        subtask.IsDone,
                        subtask.SortOrder,
                        subtask.CreatedAt,
                        subtask.CompletedAt
                    })
                    .ToList(),
                Comments = x.Comments
                    .OrderByDescending(comment => comment.CreatedAt)
                    .Select(comment => new
                    {
                        comment.Id,
                        comment.Content,
                        comment.CreatedAt,
                        UserName = comment.User.FullName,
                        UserEmail = comment.User.Email
                    })
                    .ToList(),
                Activities = x.Activities
                    .OrderByDescending(activity => activity.CreatedAt)
                    .Select(activity => new
                    {
                        activity.Id,
                        activity.Action,
                        activity.Details,
                        activity.CreatedAt,
                        UserName = activity.User.FullName
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    private async Task<TaskItem?> FindOwnedTask(Guid id, Guid userId)
    {
        return await _dbContext.Tasks
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId);
    }

    private async Task<bool> OwnsProject(Guid userId, Guid projectId)
    {
        return await _dbContext.Projects
            .AnyAsync(x =>
                x.Id == projectId &&
                x.OwnerId == userId);
    }

    private async Task SyncTags(
        TaskItem task,
        Guid userId,
        IEnumerable<string> tagNames)
    {
        var normalizedNames = tagNames
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var existingTaskTags = await _dbContext.TaskTags
            .Where(x => x.TaskItemId == task.Id)
            .ToListAsync();

        _dbContext.TaskTags.RemoveRange(existingTaskTags);

        foreach (var tagName in normalizedNames)
        {
            var tag = await _dbContext.Tags
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Name.ToLower() == tagName.ToLower());

            if (tag == null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = tagName,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Tags.Add(tag);
            }

            _dbContext.TaskTags.Add(new TaskTag
            {
                TaskItemId = task.Id,
                TagId = tag.Id
            });
        }
    }

    private void AddActivity(
        Guid taskId,
        Guid userId,
        string action,
        string? details)
    {
        _dbContext.TaskActivities.Add(new TaskActivity
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId,
            UserId = userId,
            Action = action,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }

    private Guid CurrentUserId()
    {
        return Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
    }

    private static string Csv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
