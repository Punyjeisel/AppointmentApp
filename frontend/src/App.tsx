import { useState } from 'react'
import './App.css'

// Función auxiliar para formatear una fecha ISO en hora legible (ej. "9:00 AM")
function formatearHora(fechaISO: string): string {
  const fecha = new Date(fechaISO)
  return fecha.toLocaleTimeString('es-DO', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}

// Función auxiliar para formatear una fecha ISO en fecha legible (ej. "lunes, 23 de noviembre")
function formatearFecha(fechaISO: string): string {
  const fecha = new Date(fechaISO)
  return fecha.toLocaleDateString('es-DO', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

function App() {
  // Estado de la aplicación
  const [fechaSeleccionada, setFechaSeleccionada] = useState<string>('')
  const [horariosDisponibles, setHorariosDisponibles] = useState<any[]>([])
  const [horarioSeleccionado, setHorarioSeleccionado] = useState<string>('')

  // Estado del formulario de cita (se muestra solo después de elegir horario)
  const [nombre, setNombre] = useState('')
  const [email, setEmail] = useState('')
  const [resultadoCita, setResultadoCita] = useState<string>('')

  // FUNCIÓN 1: Consultar horarios disponibles para la fecha elegida
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

      // Filtramos solo los que están disponibles (isAvailable === true)
      const soloDisponibles = datos.filter((slot: any) => slot.isAvailable)
      setHorariosDisponibles(soloDisponibles)
    } catch (error) {
      alert('Error al consultar horarios. ¿Está corriendo el backend?')
      console.error(error)
    }
  }

  // FUNCIÓN 2: El usuario hace clic en un horario disponible
  function seleccionarHorario(start: string) {
    setHorarioSeleccionado(start)
    setResultadoCita('') // Limpiamos mensajes previos
  }

  // FUNCIÓN 3: Enviar la cita al backend
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
        // Limpiamos el formulario y el horario seleccionado
        setNombre('')
        setEmail('')
        setHorarioSeleccionado('')
        setHorariosDisponibles([])
      } else {
        const error = await respuesta.json()
        setResultadoCita(`❌ Error: ${error.error || 'No se pudo crear la cita'}`)
      }
    } catch (error) {
      setResultadoCita('❌ Error de conexión con el servidor')
      console.error(error)
    }
  }

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif', maxWidth: '600px', margin: '0 auto' }}>
      <h1>AppointmentApp</h1>

      {/* ===================== PASO 1: ELEGIR FECHA ===================== */}
      <div style={{ marginBottom: '30px' }}>
        <h2>Paso 1: Elige una fecha</h2>
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

      {/* ===================== PASO 2: ELEGIR HORARIO ===================== */}
      {horariosDisponibles.length > 0 && (
        <div style={{ marginBottom: '30px' }}>
          <h2>Paso 2: Elige un horario disponible</h2>
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

      {/* ===================== PASO 3: FORMULARIO DE CITA ===================== */}
      {horarioSeleccionado && (
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px', backgroundColor: '#f5f5f5' }}>
          <h2>Paso 3: Confirma tu cita</h2>
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

          <button
            onClick={confirmarCita}
            style={{ padding: '10px 20px', backgroundColor: '#4caf50', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '1em', fontWeight: 'bold' }}
          >
            Confirmar cita
          </button>

          {resultadoCita && (
            <div style={{ marginTop: '12px', padding: '12px', borderRadius: '4px',
              backgroundColor: resultadoCita.startsWith('✅') ? '#d4edda' : '#f8d7da',
              color: resultadoCita.startsWith('✅') ? '#155724' : '#721c24' }}>
              {resultadoCita}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default App
