import { useState } from 'react'
import './App.css'

// ================= FUNCIONES AUXILIARES =================

// Función auxiliar para parsear fechas de forma robusta desde el backend
// Extrae los componentes numéricos y construye la fecha en UTC para evitar
// problemas de zona horaria y formato.
function parsearFechaSegura(fechaISO: string): Date | null {
  if (!fechaISO || typeof fechaISO !== 'string') return null

  // Limpiar espacios
  const limpio = fechaISO.trim()

  // Intento 1: parseo directo de JavaScript
  let fecha = new Date(limpio)
  if (!isNaN(fecha.getTime())) return fecha

  // Intento 2: Extraer componentes numéricos con regex
  // Soporta: 2026-11-23T09:00:00, 2026-11-23 09:00:00, 2026-11-23T09:00:00.000Z, etc.
  const match = limpio.match(/^(\d{4})[-/](\d{2})[-/](\d{2})[T\s](\d{2}):(\d{2}):(\d{2})/)
  if (match) {
    const [, año, mes, dia, hora, minuto, segundo] = match
    fecha = new Date(Date.UTC(
      parseInt(año, 10),
      parseInt(mes, 10) - 1,
      parseInt(dia, 10),
      parseInt(hora, 10),
      parseInt(minuto, 10),
      parseInt(segundo, 10)
    ))
    if (!isNaN(fecha.getTime())) return fecha
  }

  // Intento 3: Si solo hay fecha (YYYY-MM-DD)
  const matchSoloFecha = limpio.match(/^(\d{4})[-/](\d{2})[-/](\d{2})$/)
  if (matchSoloFecha) {
    const [, año, mes, dia] = matchSoloFecha
    fecha = new Date(Date.UTC(
      parseInt(año, 10),
      parseInt(mes, 10) - 1,
      parseInt(dia, 10)
    ))
    if (!isNaN(fecha.getTime())) return fecha
  }

  // Intento 4: Último recurso con JavaScript nativo después de limpiar
  fecha = new Date(limpio.replace(' ', 'T'))
  if (!isNaN(fecha.getTime())) return fecha

  return null
}

function formatearHora(fechaISO: string): string {
  const fecha = parsearFechaSegura(fechaISO)
  if (!fecha) return 'Hora inválida'
  return fecha.toLocaleTimeString('es-DO', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}

function formatearFecha(fechaISO: string): string {
  const fecha = parsearFechaSegura(fechaISO)
  if (!fecha) return 'Fecha inválida'
  return fecha.toLocaleDateString('es-DO', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

type Vista = 'reservar' | 'citas'

function App() {
  // ================= ESTADO DE NAVEGACIÓN =================
  const [vistaActual, setVistaActual] = useState<Vista>('reservar')

  // ================= ESTADOS: RESERVAR CITA =================
  const [fechaSeleccionada, setFechaSeleccionada] = useState<string>('')
  const [horariosDisponibles, setHorariosDisponibles] = useState<any[]>([])
  const [horarioSeleccionado, setHorarioSeleccionado] = useState<string>('')

  const [nombre, setNombre] = useState('')
  const [email, setEmail] = useState('')
  const [resultadoCita, setResultadoCita] = useState<string>('')

  // ================= ESTADOS: MIS CITAS =================
  const [emailMisCitas, setEmailMisCitas] = useState<string>('')
  const [citasMisCitas, setCitasMisCitas] = useState<any[]>([])
  const [resultadoMisCitas, setResultadoMisCitas] = useState<string>('')

  // ================= FUNCIONES: NAVEGACIÓN =================

  function irAReservar() {
    setVistaActual('reservar')
    setResultadoCita('')
    setResultadoMisCitas('')
  }

  function irAMisCitas() {
    setVistaActual('citas')
    setResultadoCita('')
    setResultadoMisCitas('')
  }

  // ================= FUNCIONES: RESERVAR CITA =================

  async function consultarHorarios() {
    if (!fechaSeleccionada) {
      alert('Por favor selecciona una fecha primero')
      return
    }

    try {
      const respuesta = await fetch(
        `http://localhost:5000/api/appointments/available-slots?date=${fechaSeleccionada}`
      )
      const datos = await respuesta.json()

      const soloDisponibles = datos.filter((slot: any) => slot.isAvailable)
      setHorariosDisponibles(soloDisponibles)
    } catch (error) {
      alert('Error al consultar horarios. ¿Está corriendo el backend?')
      console.error(error)
    }
  }

  function seleccionarHorario(start: string) {
    setHorarioSeleccionado(start)
    setResultadoCita('')
  }

  async function confirmarCita() {
    if (!nombre || !email || !horarioSeleccionado) {
      alert('Por favor completa nombre y email')
      return
    }

    try {
      const respuesta = await fetch('http://localhost:5000/api/appointments', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          Start: horarioSeleccionado,
          ClientName: nombre,
          ClientEmail: email
        })
      })

      if (respuesta.ok) {
        const datos = await respuesta.json()
        setResultadoCita(`✅ Cita confirmada para ${nombre} el ${formatearFecha(datos.start)} a las ${formatearHora(datos.start)}`)
        setNombre('')
        setEmail('')
        setHorarioSeleccionado('')
        setHorariosDisponibles([])
        setFechaSeleccionada('')
      } else {
        const error = await respuesta.json()
        setResultadoCita(`❌ Error: ${error.error || 'No se pudo crear la cita'}`)
      }
    } catch (error) {
      setResultadoCita('❌ Error de conexión con el servidor')
      console.error(error)
    }
  }

  function resetearReserva() {
    setFechaSeleccionada('')
    setHorariosDisponibles([])
    setHorarioSeleccionado('')
    setNombre('')
    setEmail('')
    setResultadoCita('')
  }

  // ================= FUNCIONES: MIS CITAS =================

  async function consultarMisCitas() {
    if (!emailMisCitas) {
      setResultadoMisCitas('Por favor ingresa tu email.')
      return
    }

    try {
      const respuesta = await fetch(
        `http://localhost:5000/api/appointments/by-email?email=${encodeURIComponent(emailMisCitas)}`
      )

      if (!respuesta.ok) {
        let mensajeError = `Error del servidor (${respuesta.status})`
        try {
          const texto = await respuesta.text()
          if (texto) {
            try {
              const error = JSON.parse(texto)
              mensajeError = error.error || mensajeError
            } catch {
              mensajeError = texto.length > 100 ? texto.substring(0, 100) + '...' : texto
            }
          }
        } catch {
          // Usar mensaje genérico
        }
        setResultadoMisCitas(`❌ ${mensajeError}`)
        setCitasMisCitas([])
        return
      }

      const datos = await respuesta.json()
      setCitasMisCitas(datos)
      setResultadoMisCitas(datos.length === 0 ? 'No tienes citas registradas con este email.' : `${datos.length} cita(s) encontrada(s).`)
    } catch (error: any) {
      setResultadoMisCitas(`❌ Error de conexión: ${error.message || 'Verifica que el backend esté corriendo en localhost:5000'}`)
      console.error(error)
    }
  }

  // ================= RENDERIZADO =================

  return (
    <div className="app-container">
      <header className="app-header">
        <h1>AppointmentApp</h1>
      </header>

      {/* ===================== NAVEGACIÓN PRINCIPAL ===================== */}
      <nav className="nav">
        <button
          className={`nav-btn ${vistaActual === 'reservar' ? 'active' : ''}`}
          onClick={irAReservar}
        >
          Reservar cita
        </button>
        <button
          className={`nav-btn ${vistaActual === 'citas' ? 'active' : ''}`}
          onClick={irAMisCitas}
        >
          Mis citas
        </button>
      </nav>

      {/* ===================== VISTA: RESERVAR CITA ===================== */}
      {vistaActual === 'reservar' && (
        <section className="section reservar-section">
          <h2>Reservar una cita nueva</h2>

          {/* Paso 1: Elegir fecha */}
          {!horarioSeleccionado && (
            <div className="step">
              <h3>Paso 1: Elige una fecha</h3>
              <div className="field-row">
                <input
                  type="date"
                  value={fechaSeleccionada}
                  onChange={(e) => setFechaSeleccionada(e.target.value)}
                  className="input-date"
                />
                <button
                  className="btn btn-primary"
                  onClick={consultarHorarios}
                >
                  Ver horarios disponibles
                </button>
              </div>
            </div>
          )}

          {/* Paso 2: Elegir horario */}
          {horariosDisponibles.length > 0 && !horarioSeleccionado && (
            <div className="step">
              <h3>Paso 2: Elige un horario disponible</h3>
              <p className="step-hint">
                Selecciona la hora que prefieras para tu cita:
              </p>
              <ul className="slots-list">
                {horariosDisponibles.map((slot) => {
                  const horaInicio = formatearHora(slot.start)
                  return (
                    <li
                      key={slot.start}
                      className={`slot-item ${horarioSeleccionado === slot.start ? 'selected' : ''}`}
                      onClick={() => seleccionarHorario(slot.start)}
                    >
                      {horaInicio}
                    </li>
                  )
                })}
              </ul>
            </div>
          )}

          {/* Paso 3: Formulario de nombre y email */}
          {horarioSeleccionado && (
            <div className="step form-card">
              <h3>Paso 3: Confirma tu cita</h3>
              <p className="step-summary">
                Has seleccionado: <span className="highlight">{formatearHora(horarioSeleccionado)}</span>
              </p>

              <div className="field-group">
                <label className="field-label">Tu nombre:</label>
                <input
                  type="text"
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                  placeholder="Ej: Juan Pérez"
                  className="input-text"
                />
              </div>

              <div className="field-group">
                <label className="field-label">Tu email:</label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Ej: juan@email.com"
                  className="input-text"
                />
              </div>

              <div className="button-group">
                <button
                  className="btn btn-success"
                  onClick={confirmarCita}
                >
                  Confirmar cita
                </button>
                <button
                  className="btn btn-secondary"
                  onClick={resetearReserva}
                >
                  Cambiar horario
                </button>
              </div>

              {resultadoCita && (
                <div
                  className={`alert ${resultadoCita.startsWith('✅') ? 'alert-success' : 'alert-error'}`}
                >
                  {resultadoCita}
                </div>
              )}
            </div>
          )}
        </section>
      )}

      {/* ===================== VISTA: MIS CITAS ===================== */}
      {vistaActual === 'citas' && (
        <section className="section citas-section">
          <h2>Mis citas</h2>

          <div className="field-group">
            <input
              type="email"
              value={emailMisCitas}
              onChange={(e) => setEmailMisCitas(e.target.value)}
              placeholder="Ingresa tu email (ej. juan@email.com)"
              className="input-text"
            />
            <button
              className="btn btn-primary"
              onClick={consultarMisCitas}
              style={{ marginTop: 'var(--spacing)' }}
            >
              Consultar mis citas
            </button>
          </div>

          {resultadoMisCitas && (
            <div
              className={`alert ${resultadoMisCitas.startsWith('❌') ? 'alert-error' : 'alert-success'}`}
            >
              {resultadoMisCitas}
            </div>
          )}

          {citasMisCitas.length > 0 && (
            <div className="citas-lista">
              <h3>Tus citas:</h3>
              <ul className="citas-list">
                {citasMisCitas.map((cita) => (
                  <li
                    key={`${cita.Start}-${cita.ClientName}`}
                    className="cita-item"
                  >
                    <div className="cita-info">
                      <strong>{formatearFecha(cita.Start)}</strong>
                      <span className="cita-hora">{formatearHora(cita.Start)}</span>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>
      )}
    </div>
  )
}

export default App
