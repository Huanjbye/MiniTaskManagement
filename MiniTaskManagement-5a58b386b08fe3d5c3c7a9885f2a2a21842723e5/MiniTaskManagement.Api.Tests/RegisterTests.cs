using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests;

public class RegisterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RegisterTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_WithValidPayload_ReturnsSuccessAndCreatesUser()
    {
        // Arrange
        var email = $"register_{Guid.NewGuid():N}@example.com";
        var request = new
        {
            fullName = "Test User",
            email,
            password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ToLowerInvariant().Should().Contain("register successful");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var request = new
        {
            fullName = "Test User",
            email,
            password = "Password123!"
        };

        // Act
        await _client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        duplicateResponse.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        var body = await duplicateResponse.Content.ReadAsStringAsync();
        body.ToLowerInvariant().Should().Contain("email already exists");
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequestOrValidationError()
    {
        // Arrange
        var email = $"shortpw_{Guid.NewGuid():N}@example.com";

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Abc123"
        });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Act: Gửi request thiếu trường email
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }
}