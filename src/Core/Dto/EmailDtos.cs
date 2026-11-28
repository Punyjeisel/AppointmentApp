namespace AppointmentApp.Core.Dto;

// Datos necesarios para armar el correo de una cita.
public sealed class CitaEmailDto
{
    public string Destinatario { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
}
