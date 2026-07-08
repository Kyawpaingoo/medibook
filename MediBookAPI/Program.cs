using Data.Models;
using Infra.Services;
using Infra.Services.JwtTokens;
using Infra.UnitOfWork;
using Infra.Utility;
using MediBookAPI.Seeding;
using MediBookAPI.Services.AuthServices;
using MediBookAPI.Services.DoctorServices;
using MediBookAPI.Services.HealthServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using MediBookAPI.Services.AppointmentServices;
using MediBookAPI.Services.SlotServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    // Infers the JWT bearer scheme from the AddJwtBearer() setup below and adds the
    // "Authorize" button in Swagger UI, so protected endpoints like /api/auth/revoke can be tested.
    options.InferSecuritySchemes();
});

builder.Services.AddDbContext<BookingDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<SlotAvailabilityHelper>();
builder.Services.AddScoped<IBookingUnitOfWork, BookingUnitOfWork>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

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
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MediBook API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "MediBook API - Swagger Documentation";
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DbSeeder.SeedSuperAdminAsync(app.Services);

app.Run();
