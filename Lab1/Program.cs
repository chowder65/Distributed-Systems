using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetroGameExchange.Data;
using RetroGameExchange.Dtos;
using RetroGameExchange.Endpoints;
using RetroGameExchange.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var jwtKey = builder.Configuration["JwtKey"]
    ?? throw new InvalidOperationException("JwtKey is missing.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException("JwtKey must be at least 32 characters.");

var jwtSettings = new JwtSettings
{
    Key = jwtKey,
    Issuer = builder.Configuration["JwtIssuer"]
        ?? throw new InvalidOperationException("JwtIssuer is missing."),
    Audience = builder.Configuration["JwtAudience"]
        ?? throw new InvalidOperationException("JwtAudience is missing."),
    ExpiresMinutes = builder.Configuration.GetValue("JwtExpiresMinutes", 120)
};

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<TokenService>();

var connectionString = builder.Configuration["ConnectionStringDefault"]
    ?? throw new InvalidOperationException("ConnectionStringDefault is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new ErrorResponse(401, "Unauthorized", "Authentication is required."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new ErrorResponse(403, "Forbidden", "You are not allowed to perform this action."));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (SqliteException) { }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapApiEndpoints();

app.Run();
