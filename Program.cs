using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestaoOficina.Data;
using GestaoOficina.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GestaoOficina.Features.Onboarding;
using GestaoOficina.Features.Tenants;
using GestaoOficina.Features.Users;
using GestaoOficina.Features.Units;
using GestaoOficina.Features.Customers;
using GestaoOficina.Features.Vehicles;
using GestaoOficina.Features.ServiceOrders;
using DotNetEnv;
using GestaoOficina.Features.Auth;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddOpenApi();

var connectionString =
    $"server={Environment.GetEnvironmentVariable("DB_SERVER") ?? "127.0.0.1"};" +
    $"port={Environment.GetEnvironmentVariable("DB_PORT") ?? "3306"};" +
    $"database={Environment.GetEnvironmentVariable("DB_DATABASE") ?? "GestaoOficina"};" +
    $"user={Environment.GetEnvironmentVariable("DB_USER") ?? "root"};" +
    $"password={Environment.GetEnvironmentVariable("DB_PASSWORD") ?? ""};" +
    "allowpublickeyretrieval=true;sslmode=none";

var corsOriginsRaw = Environment.GetEnvironmentVariable("CORS_ORIGINS") ?? "http://localhost:4200";
var corsOrigins = corsOriginsRaw
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "sua-chave-secreta-bem-grande";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "GestaoOficina";
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
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<TenantPlanValidator>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<ServiceOrderService>();
builder.Services.AddScoped<ServiceOrderPdfService>();
builder.Services.AddScoped<ServiceOrderExcelService>();
builder.Services.AddScoped<PasswordResetService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
