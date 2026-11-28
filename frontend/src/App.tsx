import { useState } from 'react'
import './App.css'

// ================= FUNCIONES AUXILIARES DE FECHA =================
// Extraen fecha y hora con regex directamente del string ISO,
// sin depender de new Date() que falla con muchos formatos.

const MESES_ES = [
  'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
  'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'
]

const DIAS_SEMANA_ES = [
  'domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'
]

function extraerHora(fechaISO: string): string {
  if (!fechaISO || typeof fechaISO !== 'string') return 'Hora inválida'
  const match = fechaISO.match(/T(\d{2}):(\d{2})/)
  if (match) {
    const hora24 = parseInt(match[1], 10)
    const minuto = match[2]
    const hora12 = hora24 % 12 || 12
    const ampm = hora24 < 12 ? 'AM' : 'PM'
    return `${hora12}:${minuto} ${ampm}`
  }
  return 'Hora inválida'
}

function extraerFecha(fechaISO: string): string {
  if (!fechaISO || typeof fechaISO !== 'string') return 'Fecha inválida'
  const match = fechaISO.match(/(\d{4})-(\d{1,2})-(\d{1,2})/)
  if (match) {
    const [, añoStr, mesStr, diaStr] = match
    const año = parseInt(añoStr, 10)
    const mes = parseInt(mesStr, 10)
    const dia = parseInt(diaStr, 10)
    const fecha = new Date(año, mes - 1, dia)
    const diaSemana = DIAS_SEMANA_ES[fecha.getDay()]
    return `${diaSemana}, ${dia} de ${MESES_ES[mes - 1]}`
  }
  return 'Fecha inválida'
}

function formatearHora(fechaISO: string): string {
  return extraerHora(fechaISO)
}

function formatearFecha(fechaISO: string): string {
  return extraerFecha(fechaISO)
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
