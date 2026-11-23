namespace AppointmentApp.Core.Dto;

// DTO de respuesta para una cita. No expone el Id interno ni datos sensibles.
public sealed class AppointmentDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string ClientName { get; set; } = string.Empty;
}

// DTO de solicitud para crear una cita. Solo los datos que el cliente debe enviar.
public sealed class CreateAppointmentRequest
{
    public DateTime Start { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
}

// DTO de respuesta al crear una cita. Información útil para el cliente, sin Id interno.
public sealed class AppointmentCreatedResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string ClientName { get; set; } = string.Empty;
}

// DTO de respuesta para un horario disponible.
public sealed class AvailabilitySlotDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAvailable { get; set; }
    public int DurationMinutes { get; set; }
}

// DTO de respuesta genérica para operaciones de cancelación y reprogramación.
public sealed class OperationResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}
