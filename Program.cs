// netstat -ano | findstr :8080
using Microsoft.Extensions.Logging;
using VCallbackServer.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<RequestLoggerService>();
builder.Services.AddControllers();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFile("logs/ServerMainInfo-{Date}.txt");

int port = 8080;
builder.WebHost.UseUrls($"http://*:{port}", $"http://0.0.0.0:{port}");

var app = builder.Build();

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();