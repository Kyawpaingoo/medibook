using Data.Models;
using Infra.UnitOfWork;
using MediBookAPI.Services.DoctorServices;
using MediBookAPI.Services.HealthServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<BookingDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingUnitOfWork, BookingUnitOfWork>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IHealthService, HealthService>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
