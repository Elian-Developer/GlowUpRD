import { useEffect, useState } from 'react'
import { buscarCitas } from '../services/citasApi'

const SLOT_MINUTES = 15

function toMinutes(value) {
  const [hour, minute] = value?.slice(0, 5).split(':').map(Number) ?? []
  return Number.isFinite(hour) && Number.isFinite(minute) ? hour * 60 + minute : null
}

function toTime(minutes) {
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`
}

function getBusyBlocks(appointments, professionalId, excludeId) {
  return appointments.filter((item) => String(item.empleadoId) === String(professionalId) && item.estado !== 'cancelled' && item.estado !== 'no_show' && String(item.id) !== String(excludeId ?? ''))
    .map((item) => {
      const start = toMinutes(item.inicio?.slice(11, 16))
      const end = toMinutes(item.fin?.slice(11, 16))
      const before = (item.servicios ?? []).reduce((sum, service) => sum + Number(service.minutosAntes ?? 0), 0)
      const after = (item.servicios ?? []).reduce((sum, service) => sum + Number(service.minutosDespues ?? 0), 0)
      return start === null || end === null ? null : { start: start - before, end: end + after }
    }).filter(Boolean)
}

function getAvailableTimes(horarios, professionals, date, duration, bufferBefore, bufferAfter, appointments, professionalId, excludeId) {
  if (!date) return []
  const day = new Date(`${date}T12:00:00`).getDay()
  const schedule = horarios?.find((item) => Number(item.diaSemana) === day)
  const opensAt = toMinutes(schedule?.abreA)
  const closesAt = toMinutes(schedule?.cierraA)
  if (!schedule || schedule.cerrado || opensAt === null || closesAt === null) return []
  const employeeTurns = professionals?.find((item) => String(item.id) === String(professionalId))?.horarios?.filter((item) => Number(item.diaSemana) === day && item.activo) ?? []
  const turns = employeeTurns.length ? employeeTurns.map((turno) => ({ start: Math.max(opensAt, toMinutes(turno.iniciaA) ?? opensAt), end: Math.min(closesAt, toMinutes(turno.terminaA) ?? closesAt) })) : [{ start: opensAt, end: closesAt }]
  const busyBlocks = getBusyBlocks(appointments, professionalId, excludeId)
  return turns.flatMap((turno) => {
    const firstStart = turno.start
    const latestStart = turno.end - Number(duration || 0) - Number(bufferBefore || 0) - Number(bufferAfter || 0)
    return Array.from({ length: Math.max(0, Math.floor((latestStart - firstStart) / SLOT_MINUTES) + 1) }, (_, index) => firstStart + index * SLOT_MINUTES)
  })
    .filter((start) => {
      const blockedStart = start - Number(bufferBefore || 0)
      const blockedEnd = start + Number(duration || 0) + Number(bufferAfter || 0)
      return !busyBlocks.some((block) => blockedStart < block.end && block.start < blockedEnd)
    }).map(toTime)
}

const emptyAppointment = {
  branchId: '',
  customerId: '',
  serviceId: '',
  professionalId: '',
  date: '',
  time: '09:00',
  duration: 60,
  bufferBefore: 0,
  bufferAfter: 0,
  status: 'confirmed',
  notes: '',
}

export default function AppointmentModal({
  appointment,
  branches,
  customers,
  professionals,
  services,
  horarios,
  negocioId,
  saving = false,
  error,
  onClose,
  onSave,
  onDelete,
}) {
  const [form, setForm] = useState(() => ({ ...emptyAppointment, ...appointment }))
  const availabilityKey = `${negocioId}-${form.date}`
  const [availability, setAvailability] = useState({ key: '', appointments: null })
  useEffect(() => {
    if (!negocioId || !form.date) return undefined
    let active = true
    buscarCitas({ negocioId, desde: form.date, hasta: form.date })
      .then((appointments) => active && setAvailability({ key: availabilityKey, appointments: appointments ?? [] }))
      .catch(() => active && setAvailability({ key: availabilityKey, appointments: null }))
    return () => { active = false }
  }, [availabilityKey, form.date, negocioId])
  useEffect(() => {
    const input = document.querySelector('.appointment-modal input[type="date"]')
    const openPicker = (event) => {
      try { event.currentTarget.showPicker?.() } catch { /* The browser retains the native date input behavior. */ }
    }
    input?.addEventListener('click', openPicker)
    return () => input?.removeEventListener('click', openPicker)
  }, [])
  const dayAppointments = availability.key === availabilityKey ? availability.appointments : null
  const availableTimes = dayAppointments === null ? [] : getAvailableTimes(horarios, professionals, form.date, form.duration, form.bufferBefore, form.bufferAfter, dayAppointments, form.professionalId, appointment?.id)
  const selectedTime = availableTimes.includes(form.time) ? form.time : ''
  const totalReserved = form.duration + form.bufferBefore + form.bufferAfter

  function update(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: name === 'duration' ? Number(value) : value }))
  }

  function selectService(event) {
    const service = services.find((item) => String(item.id) === event.target.value)
    setForm((current) => ({
      ...current,
      serviceId: event.target.value,
      duration: service?.duration ?? current.duration,
      bufferBefore: service?.bufferBefore ?? 0,
      bufferAfter: service?.bufferAfter ?? 0,
    }))
  }

  function submit(event) {
    event.preventDefault()
    onSave(form)
  }

  return (
    <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className="appointment-modal" role="dialog" aria-modal="true" aria-labelledby="appointment-title">
        <header className="modal-header">
          <div><span className="section-kicker">AGENDA</span><h2 id="appointment-title">{appointment?.id ? 'Editar cita' : 'Nueva cita'}</h2></div>
          <button type="button" onClick={onClose} aria-label="Cerrar modal">×</button>
        </header>

        <form onSubmit={submit} className="appointment-form">
          <label>
            <span>Sucursal</span>
            <select name="branchId" value={form.branchId} onChange={update} required>
              <option value="">Selecciona una sucursal</option>
              {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
            </select>
          </label>

          <label>
            <span>Cliente</span>
            <select name="customerId" value={form.customerId} onChange={update} required>
              <option value="">Selecciona un cliente</option>
              {customers.map((customer) => <option key={customer.id} value={customer.id}>{customer.name}</option>)}
            </select>
          </label>

          <label>
            <span>Servicio</span>
            <select name="serviceId" value={form.serviceId} onChange={selectService} required>
              <option value="">Selecciona un servicio</option>
              {services.map((service) => <option key={service.id} value={service.id}>{service.name} · RD$ {service.price.toLocaleString()}</option>)}
            </select>
          </label>

          <label>
            <span>Profesional</span>
            <select name="professionalId" value={form.professionalId} onChange={update} required>
              <option value="">Selecciona un profesional</option>
              {professionals.map((professional) => <option key={professional.id} value={professional.id}>{professional.name}</option>)}
            </select>
          </label>

          <div className="modal-field-row">
            <label><span>Fecha</span><input type="date" name="date" value={form.date} onChange={update} required /></label>
            <label><span>Hora</span><select name="time" value={selectedTime} onChange={update} required><option value="" disabled>{dayAppointments === null ? 'Cargando disponibilidad...' : availableTimes.length ? 'Selecciona una hora' : 'No hay horarios disponibles'}</option>{availableTimes.map((time) => <option key={time} value={time}>{time}</option>)}</select></label>
          </div>

          <div className="modal-field-row">
            <div className="appointment-duration-summary"><span>Duración reservada</span><strong>{form.duration} min de servicio</strong><small>{form.bufferBefore || form.bufferAfter ? `Buffer: ${form.bufferBefore} min antes · ${form.bufferAfter} min después` : 'Sin tiempo de buffer'}</small><b>{totalReserved} min bloqueados en agenda</b></div>
            <label><span>Estado</span><select name="status" value={form.status} onChange={update}><option value="pending">Pendiente</option><option value="confirmed">Confirmada</option><option value="completed">Completada</option><option value="cancelled">Cancelada</option><option value="no_show">No asistió</option></select></label>
          </div>

          <label><span>Notas</span><textarea name="notes" value={form.notes} onChange={update} rows="3" placeholder="Preferencias, alergias o comentarios..." /></label>

          {error && <div className="modal-error" role="alert">{error}</div>}

          <footer className="modal-actions">
            {appointment?.id && <button className="danger-button" type="button" onClick={() => onDelete(appointment.id)} disabled={saving}>Eliminar</button>}
            <span />
            <button className="secondary-button" type="button" onClick={onClose} disabled={saving}>Cancelar</button>
            <button className="save-button" type="submit" disabled={saving}>{saving ? 'Guardando...' : appointment?.id ? 'Guardar cambios' : 'Crear cita'}</button>
          </footer>
        </form>
      </section>
    </div>
  )
}
