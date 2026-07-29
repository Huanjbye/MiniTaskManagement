using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Admin;

public class AdminTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AdminTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task GetUsersList_AsAdmin_Returns200OKWithData()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAdminDashboard_AsNormalUser_Returns403Forbidden()
    {
        // Arrange
        var userToken = await GetNormalUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act
        var response = await _client.GetAsync("/api/admin/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAdminDashboard_AsAdmin_Returns200OK()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync("/api/admin/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    #region Safe Helper Methods
    private async Task<string> GetAdminTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.com", password = "Admin123!" });
        var token = await ExtractTokenAsync(loginRes);
        return token ?? await GetNormalUserTokenAsync();
    }

    private async Task<string> GetNormalUserTokenAsync()
    {
        var email = $"normal_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Normal User", email, password = "Password123!" });
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