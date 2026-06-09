using AllSports.Application.Interfaces.Auth.Repository;
using AllSports.Application.Interfaces.Auth.Services;
using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Services.Auth;
using AllSports.Application.Services.Darts;
using AllSports.Infrastructure.Persistence;
using AllSports.Infrastructure.Repositories.Auth;
using AllSports.Infrastructure.Repositories.Darts;
using AllSports.Infrastructure.Services.Auth;
using AllSports.Infrastructure.Services.Darts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AllSports.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IDartsRankingService, DartsRankingService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null)));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IDartsRankingRepository, DartsRankingRepository>();
        services.AddScoped<IDartsScraper, DartsScraper>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHashingService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var issuer = configuration["Jwt:Issuer"] ?? "AllSports";
        var audience = configuration["Jwt:Audience"] ?? "AllSports";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string policyName)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (!environment.IsDevelopment() && allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Configuration section 'Cors:AllowedOrigins' must contain at least one origin.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(IsLocalhostOrigin);
                }
                else
                {
                    policy.WithOrigins(allowedOrigins);
                }

                policy.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static bool IsLocalhostOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme is "http" or "https"
            && uri.Host is "localhost" or "127.0.0.1" or "::1";
    }
}
