using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ReservationsMicroService.Data;
using ReservationsMicroService.Middleware;
using ReservationsMicroService.Repository;
using ReservationsMicroService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// =========================
// JWT CONFIG
// =========================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret key is not configured.");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// =========================
// DATABASE CONFIG
// =========================
var databaseSettings = builder.Configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? throw new InvalidOperationException("Database settings are not configured.");

switch (databaseSettings.Provider)
{
    case "SqlServer":
        builder.Services.AddDbContext<ReservationsDbContext>(options =>
            options.UseSqlServer(databaseSettings.ConnectionStrings.SqlServer));
        break;
    case "MySQL":
        builder.Services.AddDbContext<ReservationsDbContext>(options =>
            options.UseMySql(databaseSettings.ConnectionStrings.MySQL, ServerVersion.AutoDetect(databaseSettings.ConnectionStrings.MySQL)));
        break;
    case "PostgreSQL":
        builder.Services.AddDbContext<ReservationsDbContext>(options =>
            options.UseNpgsql(databaseSettings.ConnectionStrings.PostgreSQL));
        break;
    case "SQLite":
        builder.Services.AddDbContext<ReservationsDbContext>(options =>
            options.UseSqlite(databaseSettings.ConnectionStrings.SQLite));
        break;
    case "MongoDB":
        // MongoDB-specific configuration
        break;
    default:
        throw new ArgumentException("Unsupported database provider");
}

// =========================
// SERVICES & REPOSITORIES
// =========================
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IReservationStatusRepository, ReservationStatusRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationStatusService, ReservationStatusService>();
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("UserAndAuthorizationManagementMicroService", client =>
{
    client.BaseAddress = new Uri("http://localhost:7007/"); 
})
.ConfigurePrimaryHttpMessageHandler(() => 
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

builder.Services.AddHttpClient("SendingEmailsMicroService", client =>
{
    client.BaseAddress = new Uri("http://localhost:7005/"); 
})
.ConfigurePrimaryHttpMessageHandler(() => 
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

builder.Services.AddHttpClient("RoomMicroService", client =>
{
    client.BaseAddress = new Uri("http://localhost:7002/"); 
})
.ConfigurePrimaryHttpMessageHandler(() => 
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

builder.Services.AddScoped<UserServiceClient>();
builder.Services.AddScoped<EmailServiceClient>();
builder.Services.AddScoped<RoomServiceClient>();

builder.Services.AddHttpContextAccessor();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ReservationsMicroService", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// =========================
// PIPELINE
// =========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
