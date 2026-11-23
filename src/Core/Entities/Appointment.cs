namespace AppointmentApp.Core.Entities;

// Representa una cita reservada por un cliente.
public sealed class Appointment
{
    public Guid Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
