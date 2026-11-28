namespace AppointmentApp.Presentacion.Services;

using AppointmentApp.Core.Dto;
using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

// Servicio en segundo plano que envía recordatorios 1 hora antes de cada cita.
// Ejecuta una verificación cada 5 minutos para citas que inician en ~60 minutos
// y que aún no tienen el recordatorio enviado (ReminderSent = false).
public sealed class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

    public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de recordatorios iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarRecordatoriosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando recordatorios.");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }

    private async Task ProcesarRecordatoriosAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Zona horaria del negocio: America/Santo_Domingo (UTC-4).
        var ahora = DateTime.UtcNow.AddHours(-4);
        var desde = ahora.AddMinutes(55);
        var hasta = ahora.AddMinutes(65);

        var citasPendientes = await contexto.Appointments
            .Where(a => !a.ReminderSent && a.Start >= desde && a.Start <= hasta)
            .ToListAsync(stoppingToken);

        foreach (var cita in citasPendientes)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await emailService.SendReminderAsync(cita.ClientEmail, new AppointmentDto
            {
                Start = cita.Start,
                End = cita.End,
                ClientName = cita.ClientName
            });

            cita.ReminderSent = true;
            await contexto.SaveChangesAsync(stoppingToken);
        }
    }
}
