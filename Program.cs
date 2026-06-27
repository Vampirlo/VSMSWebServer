// netstat -ano | findstr :8080
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using VSMSWebClient.Services;
using VSMSWebServer.Data;
using VSMSWebServer.Services;
using VSMSWebServer.Services.MegaLabs;
using VSMSWebServer.Services.MegaLabs.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Adding DbContext (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=requests.db"));

builder.Services.AddControllers();
builder.Services.AddSingleton<RequestLoggerService>();
builder.Services.AddScoped<RequestRepositoryService>();
builder.Services.AddSingleton<IniFileService>();
builder.Services.AddSingleton<ClientSyncStateService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFile("logs/ServerMainInfo-{Date}.txt");

var iniService = new IniFileService();
string portValue = iniService.ReadValue("VSMSWebServer", "port");
string localhostValue = iniService.ReadValue("VSMSWebServer", "localhost");
double pduPerSecond = iniService.ReadDoubleValue("VSMSWebServer", "pduPerSecond", 4.0);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHttpClient<SmsMegaLabsService>(); // реальный отправитель
builder.Services.AddSingleton<SmsQueueManager>(sp =>
{
    var real = sp.GetRequiredService<SmsMegaLabsService>();
    var logger = sp.GetRequiredService<ILogger<SmsQueueManager>>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    return new SmsQueueManager(real, logger, pduPerSecond, scopeFactory);
});
// Подменяем интерфейс ISmsMegaLabsService на SmsQueueManager
builder.Services.AddSingleton<ISmsMegaLabsService>(sp => sp.GetRequiredService<SmsQueueManager>());
// Фоновый обработчик очереди
builder.Services.AddHostedService<SmsQueueBackgroundService>();


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
        $"https://0.0.0.0:{port}",
        $"https://localhost:{port}"
    );
}
else
{
    builder.WebHost.UseUrls($"https://0.0.0.0:{port}");
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        logger.LogError(exception, "Unhandled exception in request {Path}", context.Request.Path);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    });
});

// Automatic database creation on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated(); // Creates a database and table if they do not exist
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();