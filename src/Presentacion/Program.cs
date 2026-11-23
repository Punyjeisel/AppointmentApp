using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar el servicio de disponibilidad.
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

// Registrar controladores de API.
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

// Endpoint de salud (se mantiene como minimal API por simplicidad).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Mapear rutas de los controladores (ej: /api/appointments/available-slots).
app.MapControllers();

app.Run();

public partial class Program { }
