using AppointmentApp.Core.Dto;
using AppointmentApp.Core.Interfaces;

// Doblado de prueba de IEmailService.
// Captura los últimos correos enviados para inspección en las pruebas.
public sealed class FakeEmailService : IEmailService
{
    public AppointmentDto? UltimaConfirmacion { get; private set; }
    public string? UltimoDestinatarioConfirmacion { get; private set; }

    public AppointmentDto? UltimoRecordatorio { get; private set; }
    public string? UltimoDestinatarioRecordatorio { get; private set; }

    public Task SendConfirmationAsync(string destinatario, AppointmentDto cita)
    {
        UltimoDestinatarioConfirmacion = destinatario;
        UltimaConfirmacion = cita;
        return Task.CompletedTask;
    }

    public Task SendReminderAsync(string destinatario, AppointmentDto cita)
    {
        UltimoDestinatarioRecordatorio = destinatario;
        UltimoRecordatorio = cita;
        return Task.CompletedTask;
    }
}
