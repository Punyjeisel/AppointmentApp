import { useState } from 'react'
import './App.css'

function App() {
  const [fecha, setFecha] = useState('')
  const [slots, setSlots] = useState<any[]>([])


  async function consultarDisponibilidad() {
  if (!fecha) {
    alert('Por favor selecciona una fecha')
    return
  }

  try {
    const respuesta = await fetch(
      `http://localhost:5000/api/appointments/available-slots?date=${fecha}`
    )
    const datos = await respuesta.json()
    setSlots(datos)
  } catch (error) {
    alert('Error al consultar la API. ¿Está corriendo el backend en localhost:5000?')
    console.error(error)
  }
}

  return (
  <div className="App" style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>
    <h1>AppointmentApp</h1>
    <h2>Ver horarios disponibles</h2>
    
    <div style={{ marginBottom: '20px' }}>
      <label>Selecciona una fecha:</label><br />
      <input
        type="date"
        value={fecha}
        onChange={(e) => setFecha(e.target.value)}
      />
    </div>

    <button onClick={consultarDisponibilidad}>
      Consultar disponibilidad
    </button>

    {slots.length > 0 && (
  <div style={{ marginTop: '20px' }}>
    <h3>Horarios para el {fecha}</h3>
    <ul style={{ listStyle: 'none', padding: 0 }}>
      {slots.map((slot) => (
        <li
          key={slot.start}
          style={{
            marginBottom: '8px',
            padding: '10px',
            backgroundColor: slot.isAvailable ? '#d4edda' : '#f8d7da',
            borderRadius: '4px'
          }}
        >
          {slot.start} - {slot.end} 
          {slot.isAvailable ? ' ✅ Disponible' : ' ❌ Ocupado'}
        </li>
      ))}
    </ul>
  </div>
)}
  </div>
)
}

export default App