# AGENTS.md — AppointmentApp

## Estado del repositorio
- Nivel 0-5 completados: arquitectura KISS, 5 endpoints de API, persistencia EF Core + PostgreSQL, WorkSchedules, Swagger UI
- Build: compila sin errores (verificado en Linux/WLS; pendiente compilar en Windows para Swagger)
- Pruebas: 13 pasando, 1 skippeada, 0 fallando

## Objetivo final
- Proyecto 100% funcional, listo para producción y apto para vender.
- Despliegue totalmente gratuito.
- Un segundo computador actúa como servidor que almacena la información de los usuarios (registros, citas y demás datos).

## Fuente de verdad
- `PROJECT.md`: requisitos del producto (stack, reglas de negocio, zona horaria, fuera de alcance).
- `PLAN.md`: plan de implementación.
- `.opencode/skill/socratic-code-mentor/SKILL.md`: skill de mentorado local de OpenCode.

## Stack tecnológico (no cambiar sin aprobación del usuario)
- Backend: C# con ASP.NET Core Web API (net10.0)
- Frontend: React con TypeScript (pendiente)
- Base de datos: PostgreSQL (Nivel 5)
- ORM: Entity Framework Core (Nivel 5)
- Estilo de API: REST
- Arquitectura: dos capas (Core + Presentacion)

## Estructura del proyecto (KISS)

```
AppointmentApp/
├── AppointmentApp.sln
├── PLAN.md
├── PROJECT.md
├── AGENTS.md
├── src/Core/
│   ├── AppointmentApp.Core.csproj
│   ├── Entities/Appointment.cs
│   ├── Entities/AvailabilitySlot.cs
│   ├── Entities/WorkSchedule.cs
│   ├── Interfaces/IAvailabilityService.cs
│   ├── Persistence/AppointmentDbContext.cs
│   ├── Persistence/Migrations/
│   │   ├── 20261123041051_InitialCreate.cs
│   │   ├── 20261123041051_InitialCreate.Designer.cs
│   │   ├── 20261123053504_InitialCreate.cs
│   │   ├── 20261123052855_WorkSchedules.cs
│   │   ├── 20261123052855_WorkSchedules.Designer.cs
│   │   └── AppointmentDbContextModelSnapshot.cs
│   └── Services/AvailabilityService.cs
├── src/Presentacion/
│   ├── AppointmentApp.Presentacion.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Controllers/
│       └── AppointmentsController.cs
└── tests/
    └── AppointmentApp.Tests/
        ├── AppointmentApp.Tests.csproj
        └── AvailabilityServiceTests.cs
```

**Capa Core:** entidades, interfaz de disponibilidad, servicio de disponibilidad, contexto de BD, migraciones. Contiene toda la lógica de negocio.
**Capa Presentacion:** API ASP.NET Core que referencia Core. Contiene Program.cs, Swagger UI, appsettings y la carpeta `Controllers/` con los endpoints HTTP.

## Comandos

| Comando | Propósito |
|---------|-----------|
| `export PATH="$HOME/.dotnet:$PATH"` | Activar el SDK de .NET (Linux/WSL) |
| `export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` | Evitar dependencia de libicu en Ubuntu |
| `$env:PATH = "$HOME\.dotnet;$env:PATH"` | Activar el SDK de .NET (Windows PowerShell) |
| `$env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "1"` | Evitar dependencia de libicu (Windows PowerShell) |
| `dotnet build AppointmentApp.sln` | Compilar toda la solución |
| `dotnet test AppointmentApp.sln` | Ejecutar todas las pruebas |
| `dotnet run --project src/Presentacion/AppointmentApp.Presentacion.csproj` | Ejecutar la API |
| `dotnet ef database update --project src/Core/AppointmentApp.Core.csproj --startup-project src/Presentacion/AppointmentApp.Presentacion.csproj` | Aplicar migraciones a PostgreSQL |
| `http://localhost:5000/` | Acceder a Swagger UI |

## Modo de trabajo (skill socratic-code-mentor)
- **Modo mentor activo:** el usuario escribe la lógica principal (reglas de negocio). El agente escribe todo lo demás (pruebas, stubs, conexiones, entorno).
- El usuario empieza a escribir lógica principal a partir del Nivel 1.

## Reglas de trabajo del agente (PROJECT.md §13)
1. Leer `PROJECT.md` antes de planificar o implementar funcionalidades.
2. Inspeccionar el repositorio existente antes de proponer cambios.
3. No asumir reglas de negocio no especificadas; detectar ambigüedades y preguntar.
4. Antes de implementar, presentar un plan breve con áreas y archivos a modificar; esperar aprobación.
5. Implementar de forma incremental y ejecutar las pruebas y compilaciones pertinentes.
6. Modificar `PROJECT.md` solo cuando los requisitos cambien y con aprobación del usuario.

## Reglas de negocio críticas (verificables en `PROJECT.md`)
- Zona horaria: `America/Santo_Domingo`.
- Cita: 60 minutos; mínima 1 hora de anticipación; máxima 30 días; mismo día permitido.
- No reservas duplicadas ni superpuestas; validar en el servidor.
- Cancelación: con al menos 2 horas de anticipación. Reprogramación: con al menos 24 horas.
- Correos: Gmail SMTP (confirmación y recordatorio 1 hora antes), separados de la lógica de reservas.
- Fuera de alcance inicial: múltiples negocios, cuentas de cliente, pagos, SMS/WhatsApp, roles complejos, duraciones variables.

## Próximos pasos sugeridos
1. Compilar en Windows y probar Swagger UI en `http://localhost:5000/`
2. Crear frontend React + TypeScript
3. Configurar Gmail SMTP para correos de confirmación y recordatorio
4. Desplegar en segunda PC como servidor
