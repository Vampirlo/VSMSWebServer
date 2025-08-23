// netstat -ano | findstr :8080
using Microsoft.Extensions.Logging;
using VCallbackServer.Services;
using Serilog;
using Microsoft.EntityFrameworkCore;
using VCallbackServer.Data;

var builder = WebApplication.CreateBuilder(args);

// Добавляем SQLite
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

// Автоматическое создание базы данных при запуске
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated(); // Создаёт БД и таблицу если их нет
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();