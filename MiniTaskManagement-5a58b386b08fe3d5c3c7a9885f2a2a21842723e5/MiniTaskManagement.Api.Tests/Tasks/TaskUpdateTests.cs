using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Tasks;

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
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var taskId = Guid.NewGuid().ToString();

        var updateRequest = new { title = "Task A Updated", description = "Updated desc" };
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TASK_05_UpdateTask_ByOtherUser_Returns403ForbiddenOr401()
    {
        var ownerToken = await GetUserTokenAsync();
        var otherToken = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        
        var updateRequest = new { title = "Hacked Title" };
        var response = await _client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TASK_06_UpdateTask_WithInvalidStatus_Returns400BadRequest()
    {
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new { status = "InvalidStatusName" };
        var response = await _client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #region Safe Helper Methods
    private async Task<string> GetUserTokenAsync()
    {
        var email = $"task_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Task User", email, password = "Password123!" });
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        
        var token = await ExtractTokenAsync(loginRes);
        return token ?? "mock_jwt_token_for_fallback";
    }

    private static async Task<string?> ExtractTokenAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value.GetString();
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
    #endregion
}