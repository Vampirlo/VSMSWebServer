// netstat -ano | findstr :8080
using VSMSWebServer.Services;
using Microsoft.EntityFrameworkCore;
using VSMSWebServer.Data;
using VSMSWebClient.Services;

var builder = WebApplication.CreateBuilder(args);

// Adding SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=requests.db"));

builder.Services.AddControllers();
builder.Services.AddScoped<RequestLoggerService>();
builder.Services.AddScoped<RequestRepositoryService>();
builder.Services.AddSingleton<IniFileService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFile("logs/ServerMainInfo-{Date}.txt");

var iniService = new IniFileService();
string portValue = iniService.ReadValue("VSMSWebServer", "port");
string localhostValue = iniService.ReadValue("VSMSWebServer", "localhost");

int port = 8080;

if (!string.IsNullOrEmpty(portValue) && int.TryParse(portValue, out int parsedPort))
{
    port = parsedPort;
}

bool useLocalhost = !string.IsNullOrEmpty(localhostValue) &&
                    bool.TryParse(localhostValue, out bool parsedLocalhost) &&
                    parsedLocalhost;

if (useLocalhost)
{
    builder.WebHost.UseUrls(
        $"http://0.0.0.0:{port}",
        $"http://localhost:{port}"
    );
}
else
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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