using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Persistence;
using AppointmentApp.Core.Services;
using AppointmentApp.Presentacion.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Registrar el contexto de base de datos con PostgreSQL.
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar el servicio de disponibilidad.
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

// Registrar el servicio de correo electrónico (MailKit + Gmail SMTP).
builder.Services.AddScoped<IEmailService, EmailService>();

// Registrar el servicio en segundo plano de recordatorios (1 hora antes de cada cita).
builder.Services.AddHostedService<ReminderBackgroundService>();

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

// CORS debe ir al principio para que funcione incluso cuando hay errores 500.
app.UseCors("PermitirFrontend");

app.UseHttpsRedirection();

// Manejador de errores global: devuelve el mensaje real con CORS headers.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Access-Control-Allow-Origin"] = "http://localhost:5173";
        await context.Response.WriteAsync("{\"error\": \"Error interno del servidor. Verifique que PostgreSQL esté corriendo.\"}");
    });
});

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
