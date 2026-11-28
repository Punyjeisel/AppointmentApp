using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Persistence;
using AppointmentApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Registrar el contexto de base de datos con PostgreSQL.
builder.Services.AddDbContext<AppointmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar el servicio de disponibilidad.
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

// Registrar controladores de API con serialización JSON consistente para DateTime (ISO 8601 con zona horaria).
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });

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

// Convierte DateTime a formato ISO 8601 con zona horaria (UTC) para evitar
// problemas de parseo en el frontend (Invalid Date).
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Si es Unspecified (común con Npgsql/PostgreSQL), asumir que es UTC
        if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        // Formato ISO 8601 simple y consistente que JavaScript parsea sin problemas
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
