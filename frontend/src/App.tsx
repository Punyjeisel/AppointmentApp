import { useState } from 'react'
import './App.css'

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
        setResultadoCita(`✅ Cita confirmada para ${nombre} el ${datos.start}`)
        // Limpiamos el formulario y el horario seleccionado
        setNombre('')
        setEmail('')
        setHorarioSeleccionado('')
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
          <ul style={{ listStyle: 'none', padding: 0 }}>
            {horariosDisponibles.map((slot) => (
              <li
                key={slot.start}
                style={{
                  padding: '10px',
                  marginBottom: '8px',
                  backgroundColor: '#e3f2fd',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  border: horarioSeleccionado === slot.start ? '2px solid #1976d2' : '1px solid #ccc',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center'
                }}
                onClick={() => seleccionarHorario(slot.start)}
              >
                <span>{slot.start} - {slot.end}</span>
                <span style={{ fontWeight: 'bold' }}>✅ Disponible</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* ===================== PASO 3: FORMULARIO DE CITA ===================== */}
      {horarioSeleccionado && (
        <div style={{ padding: '20px', border: '1px solid #ddd', borderRadius: '8px', backgroundColor: '#f5f5f5' }}>
          <h2>Paso 3: Confirma tu cita</h2>
          <p style={{ marginBottom: '15px', fontWeight: 'bold' }}>
            Has seleccionado: {horarioSeleccionado}
          </p>

          <div style={{ marginBottom: '12px' }}>
            <label style={{ display: 'block', marginBottom: '4px' }}>Tu nombre:</label>
            <input
              type="text"
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              placeholder="Ej: Juan Pérez"
              style={{ width: '100%', padding: '6px' }}
            />
          </div>

          <div style={{ marginBottom: '12px' }}>
            <label style={{ display: 'block', marginBottom: '4px' }}>Tu email:</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Ej: juan@email.com"
              style={{ width: '100%', padding: '6px' }}
            />
          </div>

          <button
            onClick={confirmarCita}
            style={{ padding: '8px 16px', backgroundColor: '#4caf50', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
          >
            Confirmar cita
          </button>

          {resultadoCita && (
            <div style={{ marginTop: '12px', padding: '10px', borderRadius: '4px',
              backgroundColor: resultadoCita.startsWith('✅') ? '#d4edda' : '#f8d7da' }}>
              {resultadoCita}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default App
