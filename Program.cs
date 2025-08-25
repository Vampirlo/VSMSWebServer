// netstat -ano | findstr :8080
using VSMSWebServer.Services;
using Microsoft.EntityFrameworkCore;
using VSMSWebServer.Data;

var builder = WebApplication.CreateBuilder(args);

// Adding SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=requests.db"));

builder.Services.AddControllers();
builder.Services.AddScoped<RequestLoggerService>();
builder.Services.AddScoped<RequestRepositoryService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFile("logs/ServerMainInfo-{Date}.txt");

int port = 8080;
builder.WebHost.UseUrls($"http://*:{port}", $"http://0.0.0.0:{port}");

var app = builder.Build();

// Automatic database creation on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated(); // Creates a database and table if they do not exist
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();