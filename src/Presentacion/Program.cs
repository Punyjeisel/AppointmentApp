using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Persistence;
using AppointmentApp.Core.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Registrar el contexto de base de datos con PostgreSQL.
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar el servicio de disponibilidad.
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

// Registrar controladores de API.
builder.Services.AddControllers();

// Configurar CORS para permitir peticiones desde el frontend (React).
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173",
            "https://localhost:3000",
            "https://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("PermitirFrontend");

// Endpoint de salud (se mantiene como minimal API por simplicidad).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Mapear rutas de los controladores (ej: /api/appointments/available-slots).
app.MapControllers();

app.Run();

public partial class Program { }
