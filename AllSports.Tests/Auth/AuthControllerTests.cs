using AllSports.API.Controllers;
using Xunit;
using AllSports.API.Requests.Auth;
using AllSports.Application.Interfaces.Auth.Services;
using AllSports.Application.Responses.Auth;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AllSports.Tests.Auth;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    private static readonly AuthResult FakeResult = new()
    {
        Token = "test-token",
        Email = "test@example.com",
        Role = "User",
        ExpiresAt = DateTime.UtcNow.AddMinutes(60),
    };

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAuthResult()
    {
        _authService
            .Setup(s => s.LoginAsync("test@example.com", "Password1!"))
            .ReturnsAsync(FakeResult);

        var response = await _sut.Login(new LoginRequest { Email = "test@example.com", Password = "Password1!" });

        var ok = Assert.IsType<OkObjectResult>(response);
        Assert.Equal(FakeResult, ok.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials."));

        var response = await _sut.Login(new LoginRequest { Email = "x@x.com", Password = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    [Fact]
    public async Task Login_InvalidCredentials_DoesNotLeakExceptionMessage()
    {
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Internal detail that should not leak."));

        var response = await _sut.Login(new LoginRequest { Email = "x@x.com", Password = "wrong" });

        var result = Assert.IsType<UnauthorizedObjectResult>(response);
        var body = result.Value?.ToString();
        Assert.DoesNotContain("Internal detail", body ?? string.Empty);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewUser_Returns201WithAuthResult()
    {
        _authService
            .Setup(s => s.RegisterAsync("new@example.com", "Password1!"))
            .ReturnsAsync(FakeResult);

        var response = await _sut.Register(new RegisterRequest { Email = "new@example.com", Password = "Password1!" });

        var created = Assert.IsType<CreatedAtActionResult>(response);
        Assert.Equal(FakeResult, created.Value);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        _authService
            .Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Email is already registered."));

        var response = await _sut.Register(new RegisterRequest { Email = "existing@example.com", Password = "Password1!" });

        Assert.IsType<ConflictObjectResult>(response);
    }

    [Fact]
    public async Task Register_DuplicateEmail_IncludesMessageInResponse()
    {
        _authService
            .Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Email is already registered."));

        var response = await _sut.Register(new RegisterRequest { Email = "existing@example.com", Password = "Password1!" });

        var conflict = Assert.IsType<ConflictObjectResult>(response);
        Assert.NotNull(conflict.Value);
    }
}
