using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Persistence;
using AppointmentApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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

// Configurar Swagger para probar la API desde el navegador.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AppointmentApp API",
        Version = "v1",
        Description = "Sistema de citas para pequeña empresa"
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("PermitirFrontend");

// Habilitar Swagger UI.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AppointmentApp API v1");
    c.RoutePrefix = string.Empty; // Para acceder desde la raíz: http://localhost:5000/
});

// Endpoint de salud (se mantiene como minimal API por simplicidad).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Mapear rutas de los controladores (ej: /api/appointments/available-slots).
app.MapControllers();

app.Run();

public partial class Program { }
