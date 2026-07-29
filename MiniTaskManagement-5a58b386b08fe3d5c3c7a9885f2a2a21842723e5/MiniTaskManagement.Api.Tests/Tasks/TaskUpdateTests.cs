using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class TaskUpdateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TaskUpdateTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task TASK_04_UpdateTask_ByOwner_Returns200OK()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Original Title");

        var updateRequest = new { title = "Task A Updated", description = "Updated desc" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TASK_05_UpdateTask_ByOtherUser_Returns403ForbiddenOr401()
    {
        // Arrange: Owner tạo task
        var ownerToken = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var taskId = await CreateSampleTaskAsync("Owner Task");

        // Act: Other User cố tình sửa
        var otherToken = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        
        var updateRequest = new { title = "Hacked Title" };
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TASK_06_UpdateTask_WithInvalidStatus_Returns400BadRequest()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Status Test Task");

        var updateRequest = new { status = "InvalidStatusName" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    #region Helper Methods
    private async Task<string> GetUserTokenAsync()
    {
        var email = $"task_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Task User", email, password = "Password123!" });
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        using var doc = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString() ?? doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> CreateSampleTaskAsync(string title)
    {
        var res = await _client.PostAsJsonAsync("/api/tasks", new { title, description = "Desc" });
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }
    #endregion
}