using AllSports.Application.Interfaces.Auth.Services;
using AllSports.Application.Responses.Auth;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AllSports.Infrastructure.Services.Auth;

public class SupabaseAuthService(HttpClient httpClient) : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/token?grant_type=password",
            new { email, password },
            JsonOptions);

        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var auth = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Supabase Auth.");

        return ToAuthResult(auth);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync(
            "auth/v1/signup",
            new { email, password },
            JsonOptions);

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            throw new InvalidOperationException("Email is already registered.");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Registration failed.");

        var auth = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Supabase Auth.");

        return ToAuthResult(auth);
    }

    private static AuthResult ToAuthResult(SupabaseAuthResponse auth) => new()
    {
        Token = auth.AccessToken,
        ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(auth.ExpiresAt).UtcDateTime,
        Email = auth.User?.Email ?? string.Empty,
        Role = auth.User?.Role ?? "authenticated",
    };

    private sealed class SupabaseAuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public long ExpiresAt { get; set; }
        public SupabaseUser? User { get; set; }
    }

    private sealed class SupabaseUser
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
