using System.Diagnostics.CodeAnalysis;
using AppointmentApp.Core.Entities;
using AppointmentApp.Core.Interfaces;

namespace AppointmentApp.Core.Services;

// Servicio de disponibilidad con datos en memoria (sin base de datos todavía).
// Zona horaria del negocio: America/Santo_Domingo (UTC-4, sin horario de verano).
public sealed class AvailabilityService : IAvailabilityService
{
    // Horario predeterminado del negocio por día de la semana.
    // Domingo está cerrado (apertura == cierre).
    private static readonly Dictionary<DayOfWeek, (TimeSpan Open, TimeSpan Close)> DefaultSchedule = new()
    {
        [DayOfWeek.Monday] = (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        [DayOfWeek.Tuesday] = (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        [DayOfWeek.Wednesday] = (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        [DayOfWeek.Thursday] = (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        [DayOfWeek.Friday] = (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
        [DayOfWeek.Saturday] = (new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0)),
        [DayOfWeek.Sunday] = (new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0)),
    };

    // Citas existentes en memoria (datos de prueba).
    private readonly List<Appointment> _appointments = new()
    {
        new Appointment
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Start = new DateTime(2026, 11, 23, 9, 0, 0, DateTimeKind.Unspecified),
            End = new DateTime(2026, 11, 23, 10, 0, 0, DateTimeKind.Unspecified),
            ClientName = "Juan Pérez",
            ClientEmail = "juan@example.com",
            CreatedAt = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Unspecified)
        },
        new Appointment
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Start = new DateTime(2026, 11, 23, 11, 0, 0, DateTimeKind.Unspecified),
            End = new DateTime(2026, 11, 23, 12, 0, 0, DateTimeKind.Unspecified),
            ClientName = "María García",
            ClientEmail = "maria@example.com",
            CreatedAt = new DateTime(2026, 11, 20, 11, 0, 0, DateTimeKind.Unspecified)
        }
    };

    // Bloqueos en memoria (datos de prueba).
    private readonly List<(DateTime Start, DateTime End)> _blockedSlots = new()
    {
        (new DateTime(2026, 11, 23, 13, 0, 0), new DateTime(2026, 11, 23, 14, 0, 0))
    };

    // Offset de la zona horaria del negocio: America/Santo_Domingo es UTC-4 todo el año.
    private static readonly TimeSpan BusinessTimeZoneOffset = TimeSpan.FromHours(-4);

    // Cada cita dura exactamente 60 minutos.
    public const int AppointmentDurationMinutes = 60;

    // Devuelve los horarios disponibles para una fecha.
    // Considera horario de apertura/cierre, citas existentes y bloqueos.
    public IEnumerable<AvailabilitySlot> GetAvailableSlots(DateTime date)
    {
        var localDate = date.Date;

        // Si el día no tiene horario definido o está cerrado, no hay slots.
        if (!DefaultSchedule.TryGetValue(localDate.DayOfWeek, out var schedule))
            return Enumerable.Empty<AvailabilitySlot>();

        if (schedule.Open == schedule.Close)
            return Enumerable.Empty<AvailabilitySlot>();

        var slots = new List<AvailabilitySlot>();
        var current = localDate.Date + schedule.Open;
        var endOfDay = localDate.Date + schedule.Close;

        // Generar slots de 60 minutos desde apertura hasta cierre.
        while (current.AddMinutes(AppointmentDurationMinutes) <= endOfDay)
        {
            var slotEnd = current.AddMinutes(AppointmentDurationMinutes);
            var isBlocked = _appointments.Any(a => current < a.End && slotEnd > a.Start) ||
                            _blockedSlots.Any(b => current < b.End && slotEnd > b.Start);

            slots.Add(new AvailabilitySlot
            {
                Start = current,
                End = slotEnd,
                IsAvailable = !isBlocked
            });

            current = slotEnd;
        }

        return slots;
    }

    // Devuelve las citas creadas.
    public IEnumerable<Appointment> GetAppointments() => _appointments;

    // Intenta crear una cita empezando en 'start'.
    // Valida: anticipación mínima (1h), máxima (30d), duración (60min), solapamientos.
    public bool CanCreateAppointment(DateTime start, out string? reason)
    {
        reason = null;

        var end = start.AddMinutes(AppointmentDurationMinutes); // La cita dura exactamente 60 minutos.

        // Hora actual del negocio (zona America/Santo_Domingo, UTC-4).
        var now = DateTime.UtcNow.AddHours(-4);

        // Regla: mínima 1 hora de anticipación.
        if (start < now.AddHours(1))
        {
            reason = "La cita debe reservarse con al menos 1 hora de anticipación.";
            return false;
        }

        // Regla: máxima 30 días de anticipación.
        if (start > now.AddDays(30))
        {
            reason = "La cita no puede reservarse con más de 30 días de anticipación.";
            return false;
        }

        // Regla: no solaparse con citas existentes ni bloqueos.
        // Dos citas se solapan si el inicio de una es antes del fin de la otra
        // y el fin de una es después del inicio de la otra.
        var chocaConCita = _appointments.Any(a => start < a.End && end > a.Start);
        var chocaConBloqueo = _blockedSlots.Any(b => start < b.End && end > b.Start);

        if (chocaConCita || chocaConBloqueo)
        {
            reason = "El horario ya está ocupado o bloqueado.";
            return false;
        }

        // Todo validó correctamente.
        return true;
    }

    // Agrega una cita a la memoria (uso de pruebas y Nivel 0).
    public void AddAppointment(Appointment appointment) => _appointments.Add(appointment);

    // Elimina una cita por su fecha de inicio (uso interno de la API).
    public bool RemoveAppointment(DateTime appointmentStart)
    {
        var cita = _appointments.FirstOrDefault(a => a.Start == appointmentStart);
        if (cita is null)
            return false;
        return _appointments.Remove(cita);
    }

    // NIVEL 2 — Pendiente de implementar por el usuario.
    // Regla: cancelación con al menos 2 horas de anticipación.
    public bool CanCancelAppointment(DateTime appointmentStart, out string? reason)
    {
        reason = null;
        var cita = _appointments.FirstOrDefault(a => a.Start == appointmentStart);
        DateTime now = DateTime.UtcNow.AddHours(-4);
        
        if (cita == null)
        {
            reason = "Esta cita no exsite";
            return false;
        }

        if (now > cita.Start.AddHours(-2))
        {
            reason = "Se necesita minimo 2 horas de antelacion para cancelar una cita.";
            return false;
        }

        return true; // NIVEL 2: implementar CanCancelAppointment.
    }

    // NIVEL 2 — Pendiente de implementar por el usuario.
    // Regla: reprogramación con al menos 24 horas de anticipación para la cita original.
    public bool CanRescheduleAppointment(DateTime appointmentStart, DateTime newStart, out string? reason)
    {
        reason = null;
        var cita = _appointments.FirstOrDefault(a => a.Start == appointmentStart);
        
    if (cita == null)
        {
            reason = "Esta cita no exsite";
            return false;
        }

        DateTime now = DateTime.UtcNow.AddHours(-4);
        
        if (now > cita.Start.AddHours(-24))
        {
            reason = "Se necesita minimo 24 horas de antelacion para editar una cita.";
            return false;
        }
        
        string? ignoreReason;
        return CanCreateAppointment(newStart, out ignoreReason);
    }
}
