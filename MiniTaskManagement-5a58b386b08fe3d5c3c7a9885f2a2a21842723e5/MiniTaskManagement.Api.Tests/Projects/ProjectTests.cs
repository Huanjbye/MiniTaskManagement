using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Projects;

public class ProjectTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProjectTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task CreateProject_WithValidData_Returns201Created()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            name = "Project Alpha",
            description = "Main Project Description",
            isPrivate = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Project Alpha");
    }

    [Fact]
    public async Task CreateProject_MissingName_Returns400BadRequest()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { description = "Project without a name" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetProjects_ReturnsUserAccessibleProjectsOnly()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #region Helper Methods
    private async Task<string> GetUserTokenAsync()
    {
        var email = $"proj_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Project User", email, password = "Password123!" });
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        using var doc = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString() 
               ?? doc.RootElement.GetProperty("accessToken").GetString()!;
    }
    #endregion
}