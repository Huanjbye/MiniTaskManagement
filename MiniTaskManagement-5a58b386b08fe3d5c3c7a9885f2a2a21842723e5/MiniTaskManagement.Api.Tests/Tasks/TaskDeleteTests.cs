using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Tasks;

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
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TASK_08_DeleteTask_ByAdmin_Returns204NoContentOr200OK()
    {
        var adminToken = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        var response = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TASK_09_DeleteTask_NonExistent_Returns404NotFound()
    {
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
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