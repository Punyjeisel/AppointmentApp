using AppointmentApp.Core.Dto;
using AppointmentApp.Core.Interfaces;
using Xunit;

namespace AppointmentApp.Tests;

public class EmailServiceTests
{
    // SendConfirmationAsync debe guardar el destinatario y los datos de la cita.
    [Fact]
    public void SendConfirmationAsync_GuardaDestinatarioYCita()
    {
        var fake = new FakeEmailService();
        var cita = new AppointmentDto
        {
            Start = new DateTime(2026, 11, 23, 10, 0, 0),
            End = new DateTime(2026, 11, 23, 11, 0, 0),
            ClientName = "Juan Pérez"
        };

        fake.SendConfirmationAsync("juan@example.com", cita).GetAwaiter().GetResult();

        Assert.Equal("juan@example.com", fake.UltimoDestinatarioConfirmacion);
        Assert.Equal(cita, fake.UltimaConfirmacion);
        Assert.Equal("Juan Pérez", fake.UltimaConfirmacion?.ClientName);
    }

    // SendReminderAsync debe guardar el destinatario y los datos de la cita.
    [Fact]
    public void SendReminderAsync_GuardaDestinatarioYCita()
    {
        var fake = new FakeEmailService();
        var cita = new AppointmentDto
        {
            Start = new DateTime(2026, 11, 23, 14, 0, 0),
            End = new DateTime(2026, 11, 23, 15, 0, 0),
            ClientName = "María García"
        };

        fake.SendReminderAsync("maria@example.com", cita).GetAwaiter().GetResult();

        Assert.Equal("maria@example.com", fake.UltimoDestinatarioRecordatorio);
        Assert.Equal(cita, fake.UltimoRecordatorio);
        Assert.Equal("María García", fake.UltimoRecordatorio?.ClientName);
    }

    // El contrato IEmailService permite implementaciones sin efectos secundarios reales (útil para pruebas).
    [Fact]
    public void FakeEmailService_NoLanzaExcepciones()
    {
        var fake = new FakeEmailService();
        var cita = new AppointmentDto
        {
            Start = new DateTime(2026, 11, 23, 9, 0, 0),
            End = new DateTime(2026, 11, 23, 10, 0, 0),
            ClientName = "Test"
        };

        Assert.True(fake.SendConfirmationAsync("test@test.com", cita).IsCompletedSuccessfully);
        Assert.True(fake.SendReminderAsync("test@test.com", cita).IsCompletedSuccessfully);
    }
}
