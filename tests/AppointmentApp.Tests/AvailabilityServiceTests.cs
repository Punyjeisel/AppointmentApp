using AppointmentApp.Core.Entities;
using AppointmentApp.Core.Services;
using Xunit;

namespace AppointmentApp.Tests;

public class AvailabilityServiceTests
{
    private readonly AvailabilityService _service = new();

    // Lunes 23/11/2026 debe tener slots de 09:00 a 17:00.
    [Fact]
    public void GetAvailableSlots_ReturnsSlotsForBusinessDay()
    {
        var date = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Unspecified);
        var slots = _service.GetAvailableSlots(date).ToList();

        Assert.NotEmpty(slots);
        Assert.All(slots, s => Assert.True(s.Start >= new DateTime(2026, 11, 23, 9, 0, 0)));
        Assert.All(slots, s => Assert.True(s.End <= new DateTime(2026, 11, 23, 17, 0, 0)));
        Assert.True(slots.All(s => s.DurationMinutes == 60));
    }

    // Domingo 22/11/2026 el negocio está cerrado.
    [Fact]
    public void GetAvailableSlots_ReturnsEmptyForSunday()
    {
        var date = new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Unspecified);
        var slots = _service.GetAvailableSlots(date).ToList();

        Assert.Empty(slots);
    }

    // El slot 09:00-10:00 está ocupado por Juan Pérez.
    [Fact]
    public void GetAvailableSlots_MarksExistingAppointmentAsUnavailable()
    {
        var date = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Unspecified);
        var slots = _service.GetAvailableSlots(date).ToList();

        var occupied = slots.FirstOrDefault(s => s.Start.Hour == 9 && s.Start.Minute == 0);
        Assert.NotNull(occupied);
        Assert.False(occupied.IsAvailable);
    }

    // El slot 13:00-14:00 está bloqueado.
    [Fact]
    public void GetAvailableSlots_MarksBlockedSlotAsUnavailable()
    {
        var date = new DateTime(2026, 11, 23, 0, 0, 0, DateTimeKind.Unspecified);
        var slots = _service.GetAvailableSlots(date).ToList();

        var blocked = slots.FirstOrDefault(s => s.Start.Hour == 13 && s.Start.Minute == 0);
        Assert.NotNull(blocked);
        Assert.False(blocked.IsAvailable);
    }

    // Devuelve las 2 citas de prueba.
    [Fact]
    public void GetAppointments_ReturnsSeedData()
    {
        var appointments = _service.GetAppointments().ToList();
        Assert.Equal(2, appointments.Count);
    }

    // Una cita que choca con una existente debe rechazarse.
    [Fact]
    public void CanCreateAppointment_RejectsOverlappingAppointment()
    {
        var ok = _service.CanCreateAppointment(
            new DateTime(2026, 11, 23, 9, 30, 0),
            out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
    }

    // Una cita válida (14:00-15:00 el lunes 23/11) debe aceptarse.
    [Fact]
    public void CanCreateAppointment_AcceptsValidAppointment()
    {
        var ok = _service.CanCreateAppointment(
            new DateTime(2026, 11, 23, 14, 0, 0),
            out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }

    // Menos de 1 hora de anticipación debe rechazarse.
    [Fact]
    public void CanCreateAppointment_RejectsLessThanOneHourAnticipation()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var start = ahoraLocal.AddMinutes(30);

        var ok = _service.CanCreateAppointment(start, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("1 hora", reason);
    }

    // Más de 30 días de anticipación debe rechazarse.
    [Fact]
    public void CanCreateAppointment_RejectsMoreThan30DaysAnticipation()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var start = ahoraLocal.AddDays(31);

        var ok = _service.CanCreateAppointment(start, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("30 días", reason);
    }

    // Mismo día con 2 horas de anticipación debe aceptarse.
    [Fact]
    public void CanCreateAppointment_AcceptsSameDayAppointment()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var start = ahoraLocal.AddHours(2);

        var ok = _service.CanCreateAppointment(start, out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }

    // Cancelación con menos de 2 horas de anticipación debe rechazarse.
    [Fact]
    public void CanCancelAppointment_RejectsLessThanTwoHours()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var appointmentStart = ahoraLocal.AddMinutes(90); // solo 1.5 horas

        // Agregar la cita al servicio para que exista
        _service.AddAppointment(new Appointment
        {
            Id = Guid.NewGuid(),
            Start = appointmentStart,
            End = appointmentStart.AddMinutes(60),
            ClientName = "Test Cancel",
            ClientEmail = "test@test.com",
            CreatedAt = ahoraLocal
        });

        var ok = _service.CanCancelAppointment(appointmentStart, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("2 horas", reason);
    }

    // Cancelación con exactamente 2 horas de anticipación debe aceptarse.
    [Fact]
    public void CanCancelAppointment_AcceptsTwoHoursOrMore()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var appointmentStart = ahoraLocal.AddHours(2).AddMinutes(1); // margen de 1 minuto

        // Agregar la cita al servicio para que exista
        _service.AddAppointment(new Appointment
        {
            Id = Guid.NewGuid(),
            Start = appointmentStart,
            End = appointmentStart.AddMinutes(60),
            ClientName = "Test Cancel OK",
            ClientEmail = "test@test.com",
            CreatedAt = ahoraLocal
        });

        var ok = _service.CanCancelAppointment(appointmentStart, out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }

    // Reprogramación con menos de 24 horas para la cita original debe rechazarse.
    [Fact]
    public void CanRescheduleAppointment_RejectsLessThan24Hours()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var appointmentStart = ahoraLocal.AddHours(12); // solo 12 horas
        var newStart = ahoraLocal.AddDays(2).AddHours(10);

        // Agregar la cita al servicio para que exista
        _service.AddAppointment(new Appointment
        {
            Id = Guid.NewGuid(),
            Start = appointmentStart,
            End = appointmentStart.AddMinutes(60),
            ClientName = "Test Reschedule",
            ClientEmail = "test@test.com",
            CreatedAt = ahoraLocal
        });

        var ok = _service.CanRescheduleAppointment(appointmentStart, newStart, out var reason);

        Assert.False(ok);
        Assert.NotNull(reason);
        Assert.Contains("24 horas", reason);
    }

    // Reprogramación con 24 horas o más para la cita original debe aceptarse si la nueva es válida.
    [Fact]
    public void CanRescheduleAppointment_Accepts24HoursOrMore()
    {
        var ahoraLocal = DateTime.UtcNow.AddHours(-4);
        var appointmentStart = ahoraLocal.AddDays(2); // más de 24 horas
        var newStart = ahoraLocal.AddDays(3).AddHours(10); // nueva cita válida

        // Agregar la cita al servicio para que exista
        _service.AddAppointment(new Appointment
        {
            Id = Guid.NewGuid(),
            Start = appointmentStart,
            End = appointmentStart.AddMinutes(60),
            ClientName = "Test Reschedule OK",
            ClientEmail = "test@test.com",
            CreatedAt = ahoraLocal
        });

        var ok = _service.CanRescheduleAppointment(appointmentStart, newStart, out var reason);

        Assert.True(ok);
        Assert.Null(reason);
    }
}
