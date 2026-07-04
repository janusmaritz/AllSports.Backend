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

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadRegistrationErrorAsync(response));

        var auth = await response.Content.ReadFromJsonAsync<SupabaseAuthResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Supabase Auth.");

        // With email confirmation enabled, Supabase returns the bare user object
        // (no session) — the caller must confirm via the emailed link, then log in.
        if (string.IsNullOrEmpty(auth.AccessToken))
        {
            return new AuthResult
            {
                RequiresEmailConfirmation = true,
                Email = auth.User?.Email ?? auth.Email ?? email,
                Role = "authenticated",
                ExpiresAt = DateTime.UtcNow,
            };
        }

        return ToAuthResult(auth);
    }

    private static async Task<string> ReadRegistrationErrorAsync(HttpResponseMessage response)
    {
        SupabaseErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<SupabaseErrorResponse>(JsonOptions);
        }
        catch (JsonException)
        {
            // Non-JSON error body — fall through to the defaults below.
        }

        return error?.ErrorCode switch
        {
            "user_already_exists" or "email_exists" => "Email is already registered.",
            "over_email_send_rate_limit" or "over_request_rate_limit" =>
                "Too many sign-up attempts. Please wait a few minutes and try again.",
            "weak_password" or "validation_failed" when !string.IsNullOrWhiteSpace(error.Msg) => error.Msg,
            _ => response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity
                ? "Email is already registered."
                : "Registration failed.",
        };
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

        // Present when Supabase returns a bare user object (signup pending confirmation).
        public string? Email { get; set; }
    }

    private sealed class SupabaseErrorResponse
    {
        public string? ErrorCode { get; set; }
        public string? Msg { get; set; }
    }

    private sealed class SupabaseUser
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
