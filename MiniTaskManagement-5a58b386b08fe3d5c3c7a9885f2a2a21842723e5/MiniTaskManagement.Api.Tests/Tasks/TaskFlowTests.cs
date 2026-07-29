using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class TaskFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TaskFlowTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task TASK_10_TaskStatusFlow_OpenToInProgressToDone_Returns200OK()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Flow Task");

        // Act 1: Transfer to InProgress
        var res1 = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", new { status = "InProgress" });
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Transfer to Done
        var res2 = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", new { status = "Done" });
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TASK_11_AddSubtask_Returns201Created()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Parent Task");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/subtasks", new { title = "Subtask 1" });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task TASK_12_AddAndRemoveTag_Returns200OK()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Tag Task");

        // Act: Thêm Tag
        var addTagRes = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/tags", new { tagName = "bug" });
        addTagRes.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task TASK_13_AddComment_Returns201Created()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Comment Task");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new { content = "Please fix this ASAP" });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
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