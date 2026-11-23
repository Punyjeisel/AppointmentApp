namespace AppointmentApp.Core.Entities;

// Representa un horario disponible (o no) para una cita.
public sealed class AvailabilitySlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAvailable { get; set; }
    public int DurationMinutes => (int)(End - Start).TotalMinutes;
}
