using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Services.Darts;
using AllSports.Infrastructure.Persistence;
using AllSports.Infrastructure.Services.Darts;
using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS Setup
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:4200", "http://localhost:5000", "http://localhost:5001")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

//Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Infrastructure
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IDartsScraper, DartsScraper>();

//Application
builder.Services.AddScoped<IPlayerService, PlayerService>();

// 2. Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Configure Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

// 4. Map Endpoints
app.MapControllers();

app.Run();
