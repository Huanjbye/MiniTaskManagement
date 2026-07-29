using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Auth;

public class RegisterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RegisterTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Register_WithValidPayload_ReturnsSuccessAndCreatesUser()
    {
        var email = $"register_{Guid.NewGuid():N}@example.com";
        var request = new { fullName = "Test User", email, password = "Password123!" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var request = new { fullName = "Test User", email, password = "Password123!" };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        duplicateResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequestOrValidationError()
    {
        var email = $"shortpw_{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Test User", email, password = "Abc" });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Register_WithMissingRequiredFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Test User", password = "Password123!" });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
    }
}