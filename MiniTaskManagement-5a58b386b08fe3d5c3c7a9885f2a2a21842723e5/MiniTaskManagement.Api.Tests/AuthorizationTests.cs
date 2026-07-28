using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Authorization_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authorization_WithUserRole_ReturnsForbiddenOrUnauthorized()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
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

        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authorization_WithAdminRole_ReturnsOk()
    {
        var token = await TryLoginWithAdminAsync();
        token.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authorization_WithInvalidToken_ReturnsUnauthorized()
    {
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalidbody.invalidchecksum";

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

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

    private async Task<string?> TryLoginWithAdminAsync()
    {
        var candidates = new[]
        {
            new { Email = "admin@example.com", Password = "Admin123!" },
            new { Email = "admin@example.com", Password = "Password123!" },
            new { Email = "admin@localhost.com", Password = "Admin123!" },
            new { Email = "admin@taskmanagement.com", Password = "Admin123!" }
        };

        foreach (var candidate in candidates)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = candidate.Email,
                password = candidate.Password
            });

            if (response.StatusCode != HttpStatusCode.OK)
            {
                continue;
            }

            var token = await ExtractTokenAsync(response);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        return null;
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