using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class TaskCreateTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TaskCreateTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task TASK_01_CreateTask_WithValidPayload_Returns201Created()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            title = "Task A",
            description = "Description for Task A",
            dueDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            priority = "High"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Task A");
    }

    [Fact]
    public async Task TASK_02_CreateTask_MissingTitle_Returns400BadRequest()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            description = "Task without title",
            priority = "Low"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task TASK_03_CreateTask_NonExistentProject_Returns404OrBadRequest()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            title = "Task with fake project",
            projectId = Guid.NewGuid().ToString(),
            priority = "Medium"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #region Helper Methods
    private async Task<string> GetUserTokenAsync()
    {
        var email = $"task_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Task User", email, password = "Password123!" });
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        
        using var doc = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString() 
               ?? doc.RootElement.GetProperty("accessToken").GetString()!;
    }
    #endregion
}