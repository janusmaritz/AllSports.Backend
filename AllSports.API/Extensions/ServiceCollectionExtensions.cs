using AllSports.Application.Interfaces.Auth.Services;
using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Services.Darts;
using AllSports.Infrastructure.Persistence;
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

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is missing.");
        var supabaseAnonKey = configuration["Supabase:AnonKey"]
            ?? throw new InvalidOperationException("Supabase:AnonKey is missing.");

        services.AddHttpClient<IAuthService, SupabaseAuthService>(client =>
        {
            client.BaseAddress = new Uri(supabaseUrl);
            client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        });

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IDartsRankingRepository, DartsRankingRepository>();
        services.AddScoped<IDartsScraper, DartsScraper>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is missing.");
        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Supabase:JwtSecret is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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
