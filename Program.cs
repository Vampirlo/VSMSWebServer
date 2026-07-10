// netstat -ano | findstr :8080
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using System.Text;
using VSMSWebClient.Services;
using VSMSWebServer.Data;
using VSMSWebServer.Models;
using VSMSWebServer.Services;
using VSMSWebServer.Services.JWT;
using VSMSWebServer.Services.MegaLabs;
using VSMSWebServer.Services.MegaLabs.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// JWT
var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddScoped<JwtService>();

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

string frontendCORS = iniService.ReadValue("VSMSFrontendCORS", "allowedOrigins");

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendCORS)   // разрешённый источник
              .AllowAnyMethod()                         // GET, POST, DELETE и т.д.
              .AllowAnyHeader()                         // любые заголовки
              .AllowCredentials();                      // если нужно передавать куки/токены
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставь токен: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


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

app.UseAuthentication(); // обязательно

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
    dbContext.Database.Migrate(); 

    if (!dbContext.Users.Any())
    {
        var admin = new User
        {
            Username = "admin",
            PasswordHash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes("admin"))),
            Role = "Admin"
        };

        dbContext.Users.Add(admin);
        dbContext.SaveChanges();
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();