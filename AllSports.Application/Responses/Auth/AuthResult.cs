namespace AllSports.Application.Responses.Auth;

public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool RequiresEmailConfirmation { get; set; }
}
