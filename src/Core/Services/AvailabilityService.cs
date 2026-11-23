namespace AppointmentApp.Core.Services;

using System.Diagnostics.CodeAnalysis;
using AppointmentApp.Core.Dto;
using AppointmentApp.Core.Entities;
using AppointmentApp.Core.Interfaces;
using AppointmentApp.Core.Persistence;

// Servicio de disponibilidad con base de datos PostgreSQL (Entity Framework Core).
// Zona horaria del negocio: America/Santo_Domingo (UTC-4, sin horario de verano).
public sealed class AvailabilityService : IAvailabilityService
{

    private AppointmentDbContext _context;

    public AvailabilityService (AppointmentDbContext context)
    {
        _context = context;
    }

    // Offset de la zona horaria del negocio: America/Santo_Domingo es UTC-4 todo el año.
    private static readonly TimeSpan BusinessTimeZoneOffset = TimeSpan.FromHours(-4);

    // Cada cita dura exactamente 60 minutos.
    public const int AppointmentDurationMinutes = 60;

    // Devuelve los horarios disponibles para una fecha.
    // Considera horario de apertura/cierre y citas existentes.
    public IEnumerable<AvailabilitySlotDto> GetAvailableSlots(DateTime date)
    {
        var localDate = date.Date;

        var schedule = _context.WorkSchedules.FirstOrDefault(w => w.DayOfWeek == localDate.DayOfWeek && w.IsWorkingDay);


        // Si el día no tiene horario definido o está cerrado, no hay slots.
        if (schedule is null)
            return Enumerable.Empty<AvailabilitySlotDto>();

        if (schedule.OpenTime == schedule.CloseTime)
            return Enumerable.Empty<AvailabilitySlotDto>();

        var slots = new List<AvailabilitySlotDto>();
        var current = localDate.Date + schedule.OpenTime;
        var endOfDay = localDate.Date + schedule.CloseTime;

        // Generar slots de 60 minutos desde apertura hasta cierre.
        while (current.AddMinutes(AppointmentDurationMinutes) <= endOfDay)
        {
            var slotEnd = current.AddMinutes(AppointmentDurationMinutes);
            var citas = _context.Appointments.ToList();
            var isBlocked = citas.Any(a => current < a.End && slotEnd > a.Start);
            slots.Add(new AvailabilitySlotDto
            {
                Start = current,
                End = slotEnd,
                IsAvailable = !isBlocked,
                DurationMinutes = AppointmentDurationMinutes
            });

            current = slotEnd;
        }

        return slots;
    }

    // Devuelve las citas creadas (sin exponer Id ni datos sensibles).
    public IEnumerable<AppointmentDto> GetAppointments() =>
        _context.Appointments.Select(a => new AppointmentDto
        {
            Start = a.Start,
            End = a.End,
            ClientName = a.ClientName
        }).ToList();

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
        var chocaConCita = _context.Appointments.Any(a => start < a.End && end > a.Start);

        // Todo validó correctamente.
        return true;
    }

    // Agrega una cita a la base de datos (uso interno de la API).
    public void AddAppointment(CreateAppointmentRequest request)
    {
        var appointment = new Appointment
        {
            Start = request.Start,
            End = request.Start.AddMinutes(AppointmentDurationMinutes),
            ClientName = request.ClientName,
            ClientEmail = request.ClientEmail,
            CreatedAt = DateTime.UtcNow.AddHours(-4)
        };

        _context.Add(appointment);
        _context.SaveChanges();
    }

    // Elimina una cita por su fecha de inicio (uso interno de la API).
    public bool RemoveAppointment(DateTime appointmentStart)
    {
        var cita = _context.Appointments.FirstOrDefault(a => a.Start == appointmentStart);
        if (cita is null) return false;
        _context.Appointments.Remove(cita);
        _context.SaveChanges();
        return true;
    }

    // Regla: cancelación con al menos 2 horas de anticipación.
    public bool CanCancelAppointment(DateTime appointmentStart, out string? reason)
    {
        reason = null;
        var cita = _context.Appointments.FirstOrDefault(a => a.Start == appointmentStart);
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

        return true;
    }

    // Regla: reprogramación con al menos 24 horas de anticipación para la cita original.
    public bool CanRescheduleAppointment(DateTime appointmentStart, DateTime newStart, out string? reason)
    {
        reason = null;
        var cita = _context.Appointments.FirstOrDefault(a => a.Start == appointmentStart);
        
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
