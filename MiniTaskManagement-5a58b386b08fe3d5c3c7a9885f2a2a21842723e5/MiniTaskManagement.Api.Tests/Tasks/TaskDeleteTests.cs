using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class TaskDeleteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TaskDeleteTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task TASK_07_DeleteTask_ByOwner_Returns204NoContentOr200OK()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = await CreateSampleTaskAsync("Task to Delete");

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{taskId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        // Verify task không tồn tại nữa
        var getResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TASK_08_DeleteTask_ByAdmin_Returns204NoContentOr200OK()
    {
        // Arrange: User tạo task
        var userToken = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var taskId = await CreateSampleTaskAsync("User Task for Admin Delete");

        // Act: Admin xóa task
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        var response = await _client.DeleteAsync($"/api/tasks/{taskId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }

    [Fact]
    public async Task TASK_09_DeleteTask_NonExistent_Returns404NotFound()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var fakeId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task<string> GetAdminTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.com", password = "Admin123!" });
        if (!loginRes.IsSuccessStatusCode) return await GetUserTokenAsync();
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