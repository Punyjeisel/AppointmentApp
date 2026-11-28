namespace AppointmentApp.Core.Interfaces;

using AppointmentApp.Core.Dto;

// Contrato para el servicio de correo electrónico.
// Implementado en Presentacion.Services.EmailService (MailKit + Gmail SMTP).
public interface IEmailService
{
    // Envía el correo de confirmación después de crear una cita.
    // No debe lanzar excepción que impida la creación de la cita (Enfoque A).
    Task SendConfirmationAsync(string destinatario, AppointmentDto cita);

    // Envía el correo de recordatorio 1 hora antes de la cita.
    Task SendReminderAsync(string destinatario, AppointmentDto cita);
}
