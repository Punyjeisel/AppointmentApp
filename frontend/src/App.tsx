import { useState } from 'react'
import './App.css'

// ================= FUNCIONES AUXILIARES =================

function formatearHora(fechaISO: string): string {
  const fecha = new Date(fechaISO)
  return fecha.toLocaleTimeString('es-DO', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}

function formatearFecha(fechaISO: string): string {
  const fecha = new Date(fechaISO)
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
        // Reiniciar flujo de reserva
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
        const error = await respuesta.json()
        setResultadoMisCitas(`❌ Error: ${error.error || 'No se pudieron consultar las citas'}`)
        setCitasMisCitas([])
        return
      }

      const datos = await respuesta.json()
      setCitasMisCitas(datos)
      setResultadoMisCitas(datos.length === 0 ? 'No tienes citas registradas con este email.' : `${datos.length} cita(s) encontrada(s).`)
    } catch (error) {
      setResultadoMisCitas('❌ Error de conexión con el servidor.')
      console.error(error)
    }
  }

  // ================= RENDERIZADO =================

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif', maxWidth: '800px', margin: '0 auto' }}>
      <h1>AppointmentApp</h1>

      {/* ===================== NAVEGACIÓN PRINCIPAL ===================== */}
      <div style={{ display: 'flex', gap: '15px', marginBottom: '30px', borderBottom: '1px solid #ccc', paddingBottom: '15px' }}>
        <button
          onClick={irAReservar}
          style={{
            padding: '10px 20px',
            border: 'none',
            backgroundColor: vistaActual === 'reservar' ? '#1976d2' : '#f5f5f5',
            color: vistaActual === 'reservar' ? 'white' : '#333',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: '500',
            fontSize: '1em'
          }}
        >
          Reservar cita
        </button>
        <button
          onClick={irAMisCitas}
          style={{
            padding: '10px 20px',
            border: 'none',
            backgroundColor: vistaActual === 'citas' ? '#1976d2' : '#f5f5f5',
            color: vistaActual === 'citas' ? 'white' : '#333',
            borderRadius: '4px',
            cursor: 'pointer',
            fontWeight: '500',
            fontSize: '1em'
          }}
        >
          Mis citas
        </button>
      </div>

      {/* ===================== VISTA: RESERVAR CITA ===================== */}
      {vistaActual === 'reservar' && (
        <section id="reservar-cita">
          <h2>Reservar una cita nueva</h2>

          {/* Paso 1: Elegir fecha */}
          {!horarioSeleccionado && (
            <div style={{ marginBottom: '30px' }}>
              <h3>Paso 1: Elige una fecha</h3>
              <input
                type="date"
                value={fechaSeleccionada}
                onChange={(e) => setFechaSeleccionada(e.target.value)}
              />
              <button
                onClick={consultarHorarios}
                style={{ marginLeft: '10px', padding: '6px 12px', cursor: 'pointer' }}
              >
                Ver horarios disponibles
              </button>
            </div>
          )}

          {/* Paso 2: Elegir horario */}
          {horariosDisponibles.length > 0 && !horarioSeleccionado && (
            <div style={{ marginBottom: '30px' }}>
              <h3>Paso 2: Elige un horario disponible</h3>
              <p style={{ color: '#666', marginBottom: '15px' }}>
                Selecciona la hora que prefieras para tu cita:
              </p>
              <ul style={{ listStyle: 'none', padding: 0 }}>
                {horariosDisponibles.map((slot) => {
                  const horaInicio = formatearHora(slot.start)
                  return (
                    <li
                      key={slot.start}
                      style={{
                        padding: '12px 16px',
                        marginBottom: '10px',
                        backgroundColor: '#e3f2fd',
                        borderRadius: '8px',
                        cursor: 'pointer',
                        border: horarioSeleccionado === slot.start ? '2px solid #1976d2' : '1px solid #ccc',
                        fontSize: '1.1em',
                        fontWeight: '500',
                        transition: 'background-color 0.2s'
                      }}
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
            <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px', backgroundColor: '#f5f5f5' }}>
              <h3>Paso 3: Confirma tu cita</h3>
              <p style={{ marginBottom: '15px', fontWeight: 'bold', color: '#333' }}>
                Has seleccionado: <span style={{ color: '#1976d2' }}>{formatearHora(horarioSeleccionado)}</span>
              </p>

              <div style={{ marginBottom: '12px' }}>
                <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold' }}>Tu nombre:</label>
                <input
                  type="text"
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                  placeholder="Ej: Juan Pérez"
                  style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
              </div>

              <div style={{ marginBottom: '12px' }}>
                <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold' }}>Tu email:</label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Ej: juan@email.com"
                  style={{ width: '100%', padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
              </div>

              <div style={{ display: 'flex', gap: '10px', marginTop: '10px' }}>
                <button
                  onClick={confirmarCita}
                  style={{
                    flex: 1,
                    padding: '10px 20px',
                    backgroundColor: '#4caf50',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '1em',
                    fontWeight: 'bold'
                  }}
                >
                  Confirmar cita
                </button>
                <button
                  onClick={resetearReserva}
                  style={{
                    padding: '10px 20px',
                    backgroundColor: '#e0e0e0',
                    color: '#333',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '1em'
                  }}
                >
                  Cambiar horario
                </button>
              </div>

              {resultadoCita && (
                <div style={{ marginTop: '15px', padding: '12px', borderRadius: '4px',
                  backgroundColor: resultadoCita.startsWith('✅') ? '#d4edda' : '#f8d7da',
                  color: resultadoCita.startsWith('✅') ? '#155724' : '#721c24' }}>
                  {resultadoCita}
                </div>
              )}
            </div>
          )}
        </section>
      )}

      {/* ===================== VISTA: MIS CITAS ===================== */}
      {vistaActual === 'citas' && (
        <section id="mis-citas" style={{ marginTop: '20px' }}>
          <h2>Mis citas</h2>

          <div style={{ marginBottom: '20px' }}>
            <input
              type="email"
              value={emailMisCitas}
              onChange={(e) => setEmailMisCitas(e.target.value)}
              placeholder="Ingresa tu email (ej. juan@email.com)"
              style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc', width: '320px', marginRight: '10px' }}
            />
            <button
              onClick={consultarMisCitas}
              style={{ padding: '8px 16px', backgroundColor: '#1976d2', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
            >
              Consultar mis citas
            </button>
          </div>

          {resultadoMisCitas && (
            <div style={{ marginBottom: '15px', padding: '10px', borderRadius: '4px',
              backgroundColor: resultadoMisCitas.startsWith('❌') ? '#f8d7da' : '#d4edda',
              color: resultadoMisCitas.startsWith('❌') ? '#721c24' : '#155724' }}>
              {resultadoMisCitas}
            </div>
          )}

          {citasMisCitas.length > 0 && (
            <div>
              <h3>Tus citas:</h3>
              <ul style={{ listStyle: 'none', padding: 0 }}>
                {citasMisCitas.map((cita) => (
                  <li
                    key={`${cita.Start}-${cita.ClientName}`}
                    style={{
                      padding: '12px 16px',
                      marginBottom: '10px',
                      backgroundColor: '#f8f9fa',
                      borderRadius: '8px',
                      border: '1px solid #dee2e6',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center'
                    }}
                  >
                    <div>
                      <strong>{formatearFecha(cita.Start)}</strong> - <span style={{ color: '#1976d2' }}>{formatearHora(cita.Start)}</span>
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
