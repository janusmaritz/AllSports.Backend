using AllSports.Application.Interfaces.Auth.Repository;
using Xunit;
using AllSports.Application.Interfaces.Auth.Services;
using AllSports.Application.Services.Auth;
using AllSports.Domain.Entities.Auth;
using Moq;

namespace AllSports.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    private static readonly (string Token, DateTime ExpiresAt) FakeToken =
        ("test-token", DateTime.UtcNow.AddMinutes(60));

    public AuthServiceTests()
    {
        _tokenService
            .Setup(t => t.GenerateToken(It.IsAny<AppUser>()))
            .Returns(FakeToken);

        _sut = new AuthService(_users.Object, _hasher.Object, _tokenService.Object);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResult()
    {
        var user = new AppUser { Id = 1, Email = "test@example.com", PasswordHash = "hash", Role = "User" };
        _users.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("hash", "Password1!")).Returns(true);

        var result = await _sut.LoginAsync("test@example.com", "Password1!");

        Assert.Equal("test-token", result.Token);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);
        Assert.Equal(FakeToken.ExpiresAt, result.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync("nobody@example.com", "Password1!"));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new AppUser { Email = "test@example.com", PasswordHash = "hash" };
        _users.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("hash", "WrongPass")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync("test@example.com", "WrongPass"));
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmailBeforeLookup()
    {
        _users.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync((AppUser?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync("TEST@EXAMPLE.COM", "Password1!"));

        _users.Verify(r => r.GetByEmailAsync("test@example.com"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_DoesNotRevealUserExistence()
    {
        // Both "user not found" and "wrong password" must throw the same exception
        // type with an indistinguishable message to prevent user enumeration.
        var user = new AppUser { Email = "test@example.com", PasswordHash = "hash" };
        _users.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var wrongPasswordEx = await Record.ExceptionAsync(
            () => _sut.LoginAsync("test@example.com", "WrongPass"));

        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        var notFoundEx = await Record.ExceptionAsync(
            () => _sut.LoginAsync("nobody@example.com", "Password1!"));

        Assert.IsType<UnauthorizedAccessException>(wrongPasswordEx);
        Assert.IsType<UnauthorizedAccessException>(notFoundEx);
        Assert.Equal(wrongPasswordEx.Message, notFoundEx.Message);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUserAndReturnsAuthResult()
    {
        _users.Setup(r => r.EmailExistsAsync("new@example.com")).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("Password1!")).Returns("hashed");

        var result = await _sut.RegisterAsync("new@example.com", "Password1!");

        _users.Verify(r => r.AddAsync(It.Is<AppUser>(u =>
            u.Email == "new@example.com" &&
            u.PasswordHash == "hashed" &&
            u.Role == "User")), Times.Once);

        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("test-token", result.Token);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _users.Setup(r => r.EmailExistsAsync("existing@example.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync("existing@example.com", "Password1!"));

        _users.Verify(r => r.AddAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailToLowercase()
    {
        _users.Setup(r => r.EmailExistsAsync("new@example.com")).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

        await _sut.RegisterAsync("NEW@EXAMPLE.COM", "Password1!");

        _users.Verify(r => r.AddAsync(It.Is<AppUser>(u => u.Email == "new@example.com")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_StoresHashNotPlaintextPassword()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("Password1!")).Returns("$hashed$value$");

        await _sut.RegisterAsync("new@example.com", "Password1!");

        _users.Verify(r => r.AddAsync(It.Is<AppUser>(u =>
            u.PasswordHash == "$hashed$value$" &&
            u.PasswordHash != "Password1!")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_SetsCreatedAtUtc()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

        var before = DateTime.UtcNow;
        await _sut.RegisterAsync("new@example.com", "Password1!");
        var after = DateTime.UtcNow;

        _users.Verify(r => r.AddAsync(It.Is<AppUser>(u =>
            u.CreatedAtUtc >= before && u.CreatedAtUtc <= after)), Times.Once);
    }
}
