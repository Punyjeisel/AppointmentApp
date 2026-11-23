# PLAN.md — Plan de Implementación AppointmentApp

## Estado actual del proyecto

### ✅ Nivel 0 — Arquitectura base (Completado)
- Estructura de carpetas: Core + Presentacion + Tests
- `AppointmentApp.Core.csproj`, `AppointmentApp.Presentacion.csproj`, `AppointmentApp.Tests.csproj`
- `AppointmentApp.sln` solución completa

### ✅ Nivel 1 — Endpoints básicos de API (Completado)
- `GET /api/appointments/available-slots?date=` — Ver horarios disponibles
- `POST /api/appointments` — Crear cita
- `DELETE /api/appointments/cancel?start=` — Cancelar cita (≥2h de anticipación)
- `PUT /api/appointments/reschedule?start=&newStart=` — Reprogramar cita (≥24h)

### ✅ Nivel 2 — Reglas de negocio y pruebas (Completado)
- `AvailabilityService.cs` con lógica de disponibilidad, solapamiento, cancelación y reprogramación
- `AvailabilityServiceTests.cs` con 14 pruebas (13 pasando, 1 skippeada pendiente de BD real)
- Validación de no reservas duplicadas ni superpuestas en el servidor

### ✅ Nivel 3 — Persistencia EF Core + PostgreSQL (Completado)
- `AppointmentDbContext.cs` con `DbSet<Appointment>` y `DbSet<WorkSchedule>`
- Entidad `WorkSchedule.cs` para horarios laborales configurables por día de la semana
- `Program.cs` registra `AddDbContext` con `UseNpgsql`
- `appsettings.json` con cadena de conexión real (sin secretos en git)
- `appsettings.Development.json` con credenciales reales (excluido de git)
- Migración `InitialCreate` aplicada: tablas `Appointments` y `WorkSchedules` creadas
- Migración `WorkSchedules` aplicada: tabla `WorkSchedules` creada
- Base de datos `appointmentdb` creada en PostgreSQL local
- Horarios laborales insertados en `WorkSchedules` (lunes-viernes 09:00-17:00, sábado 09:00-13:00, domingo cerrado)

### ✅ Nivel 4 — Backend listo para frontend (Completado)
- `GET /api/appointments` — Listar todas las citas existentes
- CORS configurado para `localhost:3000` y `localhost:5173` (React)
- Build: 0 errores, 0 warnings
- Pruebas: 13 pasando, 1 skippeada, 0 fallando

### ✅ Nivel 5 — Swagger UI para pruebas visuales (Completado en código, pendiente de compilar en Windows)
- `Swashbuckle.AspNetCore` 6.5.0 agregado a `AppointmentApp.Presentacion.csproj`
- `Program.cs` configura `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`
- Accesible desde `http://localhost:5000/` (raíz del servidor)
- Interfaz visual con botón "Try it out" para cada endpoint
- **Pendiente:** El usuario debe compilar en Windows (`dotnet build` + `dotnet run`) para probar Swagger

## 📋 Próximos pasos (orden sugerido)

### Opción A — Frontend React + TypeScript (Recomendado)
1. Crear proyecto React + TypeScript
2. Conectar frontend a los 5 endpoints de la API
3. Pantallas: ver horarios disponibles, crear cita, listar citas, cancelar, reprogramar
4. Estilo minimalista y oscuro (según PROJECT.md §9)

### Opción B — Correos Gmail SMTP
1. Configurar Gmail SMTP en `appsettings.json`
2. Enviar confirmación después de crear cita
3. Enviar recordatorio 1 hora antes de la cita
4. Separar envío de correos de la lógica de reservas

### Opción C — Despliegue en segunda PC (servidor)
1. Instalar .NET SDK 10 y PostgreSQL 18 en la segunda PC
2. Clonar repositorio de GitHub
3. Crear base de datos `appointmentdb` en PostgreSQL del servidor
4. Editar `appsettings.Development.json` con IP del servidor
5. Ejecutar `dotnet ef database update` en el servidor
6. Insertar horarios laborales en `WorkSchedules`
7. Ejecutar `dotnet run` en el servidor

## 📦 Comandos clave

| Comando | Propósito |
|---------|-----------|
| `dotnet build AppointmentApp.sln` | Compilar toda la solución |
| `dotnet test AppointmentApp.sln` | Ejecutar todas las pruebas |
| `dotnet run --project src/Presentacion/AppointmentApp.Presentacion.csproj` | Ejecutar la API |
| `dotnet ef database update --project src/Core/AppointmentApp.Core.csproj --startup-project src/Presentacion/AppointmentApp.Presentacion.csproj` | Aplicar migraciones a PostgreSQL |
| `http://localhost:5000/` | Acceder a Swagger UI (una vez compilado en Windows) |

## 🔗 Repositorio
- GitHub: https://github.com/Punyjeisel/AppointmentApp.git
- Último commit: `6ad23af` — feat: Agregar Swagger UI para probar API desde navegador
