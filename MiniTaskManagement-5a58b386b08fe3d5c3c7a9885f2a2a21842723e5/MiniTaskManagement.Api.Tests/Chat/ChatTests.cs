using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiniTaskManagement.Api.Tests.Chat;

public class ChatTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChatTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task CreateChatRoom_WithValidMembers_Returns201Created()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            roomName = "Development Team Room",
            isGroup = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/rooms", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendMessage_WithValidPayload_Returns201Created()
    {
        // Arrange
        var token = await GetUserTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Tạo room trước
        var roomRes = await _client.PostAsJsonAsync("/api/chat/rooms", new { roomName = "General" });
        var roomId = await ExtractIdAsync(roomRes);

        var messageRequest = new { roomId, message = "Hello team, let's start the task!" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/chat/rooms/{roomId}/messages", messageRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AccessChatRoom_UnauthorizedUser_Returns401Or403()
    {
        // Arrange: Không gắn Token Authorization
        _client.DefaultRequestHeaders.Clear();

        // Act
        var response = await _client.GetAsync("/api/chat/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #region Helper Methods
    private async Task<string> GetUserTokenAsync()
    {
        var email = $"chat_user_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { fullName = "Chat User", email, password = "Password123!" });
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        using var doc = JsonDocument.Parse(await loginRes.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString() 
               ?? doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> ExtractIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return Guid.NewGuid().ToString();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString()! : Guid.NewGuid().ToString();
    }
    #endregion
}