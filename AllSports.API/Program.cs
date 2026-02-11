using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Services.Darts;
using AllSports.Infrastructure.Persistence;
using AllSports.Infrastructure.Services.Darts;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];

    if (!string.IsNullOrEmpty(keyVaultName))
    {
        try
        {
            var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
            builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
            Console.WriteLine($"Successfully connected to Key Vault: {keyVaultName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR: Failed to connect to Key Vault: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("WARNING: 'KeyVaultName' environment variable is missing.");
    }
}

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.SetIsOriginAllowed(origin => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                      });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("WARNING: Connection String 'DefaultConnection' is NULL.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Infrastructure
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IDartsScraper, DartsScraper>();

// Application
builder.Services.AddScoped<IPlayerService, PlayerService>();

// Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.MapControllers();

app.Run();
