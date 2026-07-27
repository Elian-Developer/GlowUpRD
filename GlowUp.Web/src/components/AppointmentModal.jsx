import { useState } from 'react'

const emptyAppointment = {
  branchId: '',
  customerId: '',
  serviceId: '',
  professionalId: '',
  date: '',
  time: '09:00',
  duration: 60,
  status: 'confirmed',
  notes: '',
}

export default function AppointmentModal({
  appointment,
  branches,
  customers,
  professionals,
  services,
  saving = false,
  error,
  onClose,
  onSave,
  onDelete,
}) {
  const [form, setForm] = useState(() => ({ ...emptyAppointment, ...appointment }))

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
            <label><span>Hora</span><input type="time" name="time" min="08:00" max="19:00" step="900" value={form.time} onChange={update} required /></label>
          </div>

          <div className="modal-field-row">
            <label><span>Duración</span><select name="duration" value={form.duration} onChange={update} disabled><option value={form.duration}>{form.duration} minutos</option></select></label>
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
