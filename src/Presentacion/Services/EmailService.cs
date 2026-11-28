namespace AppointmentApp.Presentacion.Services;

using AppointmentApp.Core.Dto;
using AppointmentApp.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

// Servicio de correo electrónico con MailKit y Gmail SMTP.
// Implementa IEmailService definido en Core.
// Enfoque A: si el envío falla, no se lanza excepción; la cita sigue creada.
public sealed class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _remitenteNombre;
    private readonly string _remitenteEmail;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        var seccion = configuration.GetSection("Email");
        _smtpHost = seccion["SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.TryParse(seccion["SmtpPort"], out var puerto) ? puerto : 587;
        _smtpUser = seccion["SmtpUser"] ?? string.Empty;
        _smtpPass = seccion["SmtpPass"] ?? string.Empty;
        _remitenteNombre = seccion["RemitenteNombre"] ?? "AppointmentApp";
        _remitenteEmail = seccion["RemitenteEmail"] ?? string.Empty;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(string destinatario, AppointmentDto cita)
    {
        if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass))
        {
            _logger.LogWarning("Credenciales de Gmail no configuradas. Se omite envío de confirmación a {Destinatario}", destinatario);
            return;
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_remitenteNombre, _remitenteEmail));
        mensaje.To.Add(new MailboxAddress(string.Empty, destinatario));
        mensaje.Subject = $"Confirmación de cita - {cita.Start:dd/MM/yyyy HH:mm}";

        var cuerpo = $@"
Hola {cita.ClientName},

Tu cita ha sido confirmada:
📅 Fecha: {cita.Start:dd/MM/yyyy}
🕒 Hora: {cita.Start:HH:mm} - {cita.End:HH:mm}
📍 Duración: 60 minutos

Puedes consultar tus citas en cualquier momento usando tu correo electrónico.

Gracias por usar AppointmentApp.";
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        await EnviarAsync(mensaje);
    }

    public async Task SendReminderAsync(string destinatario, AppointmentDto cita)
    {
        if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass))
        {
            _logger.LogWarning("Credenciales de Gmail no configuradas. Se omite envío de recordatorio a {Destinatario}", destinatario);
            return;
        }

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_remitenteNombre, _remitenteEmail));
        mensaje.To.Add(new MailboxAddress(string.Empty, destinatario));
        mensaje.Subject = $"Recordatorio: tu cita inicia en 1 hora - {cita.Start:dd/MM/yyyy HH:mm}";

        var cuerpo = $@"
Hola {cita.ClientName},

Te recordamos que tienes una cita en 1 hora:
📅 Fecha: {cita.Start:dd/MM/yyyy}
🕒 Hora: {cita.Start:HH:mm} - {cita.End:HH:mm}

¡Nos vemos pronto!";

        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        await EnviarAsync(mensaje);
    }

    private async Task EnviarAsync(MimeMessage mensaje)
    {
        try
        {
            using var cliente = new SmtpClient();
            await cliente.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await cliente.AuthenticateAsync(_smtpUser, _smtpPass);
            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            var destino = (mensaje.To.FirstOrDefault() as MailboxAddress)?.Address ?? "desconocido";
            _logger.LogError(ex, "Error enviando correo a {Destinatario}", destino);
            // No se lanza: Enfoque A - el correo no bloquea la cita.
        }
    }
}
