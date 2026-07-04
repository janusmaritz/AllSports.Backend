using System.Net;
using System.Text;
using AllSports.Infrastructure.Services.Auth;
using Xunit;

namespace AllSports.Tests.Auth;

public class SupabaseAuthServiceTests
{
    private const string SessionJson = """
        {
          "access_token": "jwt-token",
          "token_type": "bearer",
          "expires_in": 3600,
          "expires_at": 1783099539,
          "user": { "id": "abc", "email": "new@example.com", "role": "authenticated" }
        }
        """;

    // With email confirmation enabled, signup returns the bare user object — no session.
    private const string PendingConfirmationJson = """
        {
          "id": "abc",
          "email": "new@example.com",
          "confirmation_sent_at": "2026-07-04T10:00:00Z",
          "role": "authenticated"
        }
        """;

    private static SupabaseAuthService CreateService(HttpStatusCode statusCode, string responseJson)
    {
        var handler = new StubHandler(statusCode, responseJson);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.supabase.co") };
        return new SupabaseAuthService(client);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_MapsSession()
    {
        var service = CreateService(HttpStatusCode.OK, SessionJson);

        var result = await service.LoginAsync("new@example.com", "Password1!");

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("new@example.com", result.Email);
        Assert.False(result.RequiresEmailConfirmation);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ThrowsUnauthorized()
    {
        var service = CreateService(HttpStatusCode.BadRequest,
            """{"error_code":"invalid_credentials","msg":"Invalid login credentials"}""");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.LoginAsync("new@example.com", "wrong"));
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ConfirmationDisabled_ReturnsSession()
    {
        var service = CreateService(HttpStatusCode.OK, SessionJson);

        var result = await service.RegisterAsync("new@example.com", "Password1!");

        Assert.Equal("jwt-token", result.Token);
        Assert.False(result.RequiresEmailConfirmation);
    }

    [Fact]
    public async Task RegisterAsync_ConfirmationPending_FlagsRequiresEmailConfirmation()
    {
        var service = CreateService(HttpStatusCode.OK, PendingConfirmationJson);

        var result = await service.RegisterAsync("new@example.com", "Password1!");

        Assert.True(result.RequiresEmailConfirmation);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal(string.Empty, result.Token);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsFriendlyMessage()
    {
        var service = CreateService(HttpStatusCode.UnprocessableEntity,
            """{"code":422,"error_code":"user_already_exists","msg":"User already registered"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync("existing@example.com", "Password1!"));

        Assert.Equal("Email is already registered.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_EmailRateLimited_ThrowsFriendlyMessage()
    {
        var service = CreateService(HttpStatusCode.TooManyRequests,
            """{"code":429,"error_code":"over_email_send_rate_limit","msg":"email rate limit exceeded"}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync("new@example.com", "Password1!"));

        Assert.Contains("Too many sign-up attempts", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_SurfacesSupabaseMessage()
    {
        var service = CreateService(HttpStatusCode.UnprocessableEntity,
            """{"code":422,"error_code":"weak_password","msg":"Password should be at least 6 characters."}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync("new@example.com", "x"));

        Assert.Equal("Password should be at least 6 characters.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_NonJsonErrorBody_ThrowsGenericMessage()
    {
        var service = CreateService(HttpStatusCode.InternalServerError, "<html>gateway error</html>");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync("new@example.com", "Password1!"));

        Assert.Equal("Registration failed.", ex.Message);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
