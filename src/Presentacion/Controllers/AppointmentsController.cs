using AppointmentApp.Core.Dto;
using AppointmentApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.Presentacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AppointmentsController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    // GET /api/appointments/available-slots?date=2026-11-23
    [HttpGet("available-slots")]
    public IActionResult GetAvailableSlots(DateTime date)
    {
        var slots = _availabilityService.GetAvailableSlots(date);
        return Ok(slots);
    }

    // GET /api/appointments
    [HttpGet]
    public IActionResult GetAppointments()
    {
        var appointments = _availabilityService.GetAppointments();
        return Ok(appointments);
    }

    // GET /api/appointments/by-email?email=juan@email.com
    [HttpGet("by-email")]
    public IActionResult GetAppointmentsByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "El email es requerido." });

        var appointments = _availabilityService.GetAppointmentsByEmail(email);
        return Ok(appointments);
    }

    // POST /api/appointments
    [HttpPost]
    public IActionResult CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var available = _availabilityService.CanCreateAppointment(request.Start, out var reason);

        if (available == false)
        {
            return BadRequest(new { error = reason });
        }

        _availabilityService.AddAppointment(request);

        var end = request.Start.AddMinutes(60);

        return Created($"/api/appointments/{request.Start:yyyy-MM-ddTHH:mm:ss}", new AppointmentCreatedResponse
        {
            Message = "Cita creada correctamente",
            Start = request.Start,
            End = end,
            ClientName = request.ClientName
        });
    }

    // DELETE /api/appointments/cancel?start=2026-11-23T10:00:00
    [HttpDelete("cancel")]
    public IActionResult DeleteAppointment(DateTime start)
    {
        var ok = _availabilityService.CanCancelAppointment(start, out var reason);

        if (!ok)
            return BadRequest(new { error = reason });

        var remove = _availabilityService.RemoveAppointment(start);

        if (!remove)
            return NotFound(new { error = "Cita no encontrada" });

        return Ok(new OperationResponse
        {
            Message = "Cita cancelada correctamente",
            Success = true
        });
    }

    // PUT /api/appointments/reschedule?start=2026-11-23T10:00:00&newStart=2026-11-23T14:00:00
    [HttpPut("reschedule")]
    public IActionResult RescheduleAppointment(DateTime start, DateTime newStart)
    {
        var ok = _availabilityService.CanRescheduleAppointment(start, newStart, out var reason);

        if (!ok)
            return BadRequest(new { error = reason });

        var remove = _availabilityService.RemoveAppointment(start);

        if (!remove)
            return NotFound(new { error = "Cita no encontrada" });

        _availabilityService.AddAppointment(new CreateAppointmentRequest
        {
            Start = newStart,
            ClientName = string.Empty,
            ClientEmail = string.Empty
        });

        return Ok(new OperationResponse
        {
            Message = "Cita reprogramada correctamente",
            Success = true
        });
    }
}
