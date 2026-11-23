using AppointmentApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Core.Persistence;

// Contexto de base de datos para citas.
// Conecta el código con PostgreSQL a través de Entity Framework Core.
public sealed class AppointmentDbContext : DbContext
{
    public AppointmentDbContext( DbContextOptions<AppointmentDbContext> options) : base(options) 
    {
    }

    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(a => a.ClientEmail).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.DayOfWeek).IsRequired();
        });
    }
}
