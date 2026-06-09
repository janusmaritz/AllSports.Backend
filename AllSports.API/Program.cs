using AllSports.API.Extensions;
using AllSports.Infrastructure.Persistence;
using Azure.Identity;

const string CorsPolicyName = "CorsPolicy";

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"]
        ?? throw new InvalidOperationException("Configuration value 'KeyVaultName' is missing.");

    var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

builder.Services.AddConfiguredCors(builder.Configuration, builder.Environment, CorsPolicyName);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseCors(CorsPolicyName);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await WarmUpDatabaseAsync(app.Services, app.Logger);
}

app.Run();

static async Task WarmUpDatabaseAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    const int maxAttempts = 5;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        if (await db.Database.CanConnectAsync())
        {
            if (attempt > 1)
                logger.LogInformation("Database warm-up succeeded on attempt {Attempt}", attempt);
            return;
        }

        if (attempt < maxAttempts)
        {
            logger.LogWarning(
                "Database warm-up attempt {Attempt}/{Max} failed. Retrying in 20s…",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(20));
        }
        else
        {
            logger.LogError(
                "Database warm-up failed after {Max} attempts. Check that LocalDB is running.",
                maxAttempts);
        }
    }
}
