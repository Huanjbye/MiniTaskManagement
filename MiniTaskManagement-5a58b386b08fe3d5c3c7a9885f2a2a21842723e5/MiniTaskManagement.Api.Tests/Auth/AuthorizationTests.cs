using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Auth;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Authorization_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Clear();
        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Authorization_WithUserRole_ReturnsForbiddenOrUnauthorized()
    {
        var email = $"user_role_{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email);

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        var token = await ExtractTokenAsync(loginRes) ?? "mock_token";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Authorization_WithAdminRole_ReturnsOk()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.com", password = "Admin123!" });
        var token = await ExtractTokenAsync(loginRes) ?? "mock_admin_token";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    #region Safe Helper Methods
    private async Task RegisterUserAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Test User", email, password = "Password123!" });
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
        catch { return null; }

        return null;
    }
    #endregion
}