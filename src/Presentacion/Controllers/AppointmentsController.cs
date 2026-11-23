using AppointmentApp.Core.Entities;
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

    [HttpGet("available-slots")]
    public IActionResult GetAvailableSlots ( DateTime date)
    {
        var slots = _availabilityService.GetAvailableSlots(date);
        return Ok(slots);
    }

    [HttpGet]
    public IActionResult GetAppointments()
    {
        var appointments = _availabilityService.GetAppointments();
        return Ok(appointments);
    }

    [HttpPost]
    public IActionResult CreateAppointment([FromBody] Appointment appointment)
    {
        var available = _availabilityService.CanCreateAppointment(appointment.Start, out var reason);

        if (available == false)
        {
            return BadRequest(new { error = reason});
        }

        appointment.End = appointment.Start.AddMinutes(60);
        _availabilityService.AddAppointment(appointment);

        return Created($"/api/appointments/{appointment.Id}", appointment);
    }

    [HttpDelete("cancel")]
    public IActionResult DeleteAppointment(DateTime start)
    {
        var ok = _availabilityService.CanCancelAppointment(start, out var reason);
        var remove = true;

        if (ok)
            remove = _availabilityService.RemoveAppointment(start);
        else
            return BadRequest( new {error = reason});

        if (remove == false)
            return NotFound( new {error = "Cita no encontrada"});
        
        return Ok( new {message = "Cita Cancelada correctamente"});
    }

    [HttpPut("reschedule")]
    public IActionResult RecheduleAppointment(DateTime start, DateTime newStart)
    {
        var ok = _availabilityService.CanRescheduleAppointment(start, newStart, out var reason);
        var remove = true;

        if (ok)
            remove = _availabilityService.RemoveAppointment(start);
        else
            return BadRequest( new {error = reason});

        if (remove == false)
            return NotFound( new {error = "Cita no encontrada"});

        var newAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            Start = newStart,
            End = newStart.AddMinutes(60),
            ClientName = string.Empty,
            ClientEmail = string.Empty,
            CreatedAt = DateTime.UtcNow.AddHours(-4)
        };

        _availabilityService.AddAppointment(newAppointment);

        return Ok(new { message = "Cita reporgramada Correctamente", Appointment = newAppointment});
    }
}



