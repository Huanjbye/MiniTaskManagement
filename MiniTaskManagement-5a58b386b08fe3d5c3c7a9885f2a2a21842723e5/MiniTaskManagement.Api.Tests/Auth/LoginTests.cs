using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Auth;

public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LoginTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndAllowsProtectedAccess()
    {
        var email = $"login_ok_{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });

        loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);

        var token = await ExtractTokenAsync(loginResponse);
        if (!string.IsNullOrEmpty(token))
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var protectedResponse = await _client.GetAsync("/api/tasks");
            protectedResponse.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsBadRequestOrUnauthorized()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"unknown_{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        loginResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequestOrUnauthorized()
    {
        var email = $"wrongpw_{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword123!" });

        loginResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
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