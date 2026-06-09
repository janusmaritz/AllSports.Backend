namespace AllSports.Application.Interfaces.Auth.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}
