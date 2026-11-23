namespace AppointmentApp.Core.Interfaces;

using AppointmentApp.Core.Dto;

// Contrato del servicio de disponibilidad.
// Implementado en Core.Services.AvailabilityService.
public interface IAvailabilityService
{
    // Devuelve los horarios disponibles para una fecha dada.
    IEnumerable<AvailabilitySlotDto> GetAvailableSlots(DateTime date);

    // Devuelve las citas ya creadas (sin exponear Id ni datos sensibles).
    IEnumerable<AppointmentDto> GetAppointments();

    // Intenta crear una cita. Devuelve true si se puede, false si no.
    // reason contiene el mensaje de error si falla.
    bool CanCreateAppointment(DateTime start, out string? reason);

    // Guarda una cita creada (uso interno de la API).
    void AddAppointment(CreateAppointmentRequest request);

    // Elimina una cita por su fecha de inicio (uso interno de la API).
    bool RemoveAppointment(DateTime appointmentStart);

    // Valida si se puede cancelar una cita. Debe ser con al menos 2 horas de anticipación.
    bool CanCancelAppointment(DateTime appointmentStart, out string? reason);

    // Valida si se puede reprogramar una cita. La original debe tener ≥ 24 horas de anticipación.
    // La nueva cita debe cumplir las reglas normales de creación.
    bool CanRescheduleAppointment(DateTime appointmentStart, DateTime newStart, out string? reason);
}
