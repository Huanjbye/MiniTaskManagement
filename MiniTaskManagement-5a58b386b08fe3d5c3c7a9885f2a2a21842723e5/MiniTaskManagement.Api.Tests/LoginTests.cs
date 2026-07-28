using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LoginTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndAllowsProtectedAccess()
    {
        var email = $"login_ok_{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await ExtractTokenAsync(loginResponse);
        token.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var protectedResponse = await _client.GetAsync("/api/tasks");

        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsBadRequestOrUnauthorized()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"unknown_{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequestOrUnauthorized()
    {
        var email = $"wrongpw_{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123!"
        });

        loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task RegisterUserAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<string?> ExtractTokenAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        return ExtractToken(doc.RootElement);
    }

    private static string? ExtractToken(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals("token", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("accessToken", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("access_token", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null;
            }

            if ((property.Name.Equals("data", StringComparison.OrdinalIgnoreCase) ||
                 property.Name.Equals("result", StringComparison.OrdinalIgnoreCase) ||
                 property.Name.Equals("payload", StringComparison.OrdinalIgnoreCase)) &&
                property.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = ExtractToken(property.Value);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }
}