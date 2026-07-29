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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminDashboard_AsNormalUser_Returns403Forbidden()
    {
        // Arrange: Đăng nhập bằng tài khoản User thường
        var userToken = await GetNormalUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act: Cố gắng gọi API Dashboard của Admin
        var response = await _client.GetAsync("/api/admin/dashboard");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #region Helper Methods
    private async Task<string> GetAdminTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.com", password = "Admin123!" });
        if (!loginRes.IsSuccessStatusCode)
        {
            return await GetNormalUserTokenAsync();
        }
        using var doc = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString() 
               ?? doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> GetNormalUserTokenAsync()
    {
        var email = $"normal_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Normal User", email, password = "Password123!" });
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        using var doc = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString() 
               ?? doc.RootElement.GetProperty("accessToken").GetString()!;
    }
    #endregion
}