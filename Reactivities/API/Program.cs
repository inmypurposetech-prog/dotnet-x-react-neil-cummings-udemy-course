using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    // connection string refers to databases being connected to so there is potential for multiple connection strings to exist here
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// CORS related service
builder.Services.AddCors();

var app = builder.Build();

// Middleware for CORS
app.UseCors(options => options.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:3000", "https://localhost;3000"));

// Configure the HTTP request pipeline.
app.MapControllers();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedData(context);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during migration.");
}

app.Run();
