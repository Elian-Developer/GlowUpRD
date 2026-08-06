import { useState } from 'react'

const emptyService = {
  nombre: '',
  descripcion: '',
  duracionMinutos: 60,
  precio: '',
  minutosAntes: 0,
  minutosDespues: 0,
  activo: true,
}

export default function ServiceModal({
  service,
  saving = false,
  error,
  onClose,
  onSave,
}) {
  const [form, setForm] = useState(() => ({ ...emptyService, ...service }))
  const isEdit = Boolean(service?.id)

  function update(event) {
    const { name, value, type, checked } = event.target
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }))
  }

  function toggleActive() { setForm((current) => ({ ...current, activo: !current.activo })) }

  function submit(event) {
    event.preventDefault()
    onSave(form)
  }

  function removeService() {
    if (!window.confirm(`¿Eliminar el servicio ${form.nombre}? Dejará de aparecer al crear nuevas citas.`)) return
    onSave({ ...form, eliminar: true })
  }

  return (
    <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className="appointment-modal" role="dialog" aria-modal="true" aria-labelledby="service-title">
        <header className="modal-header">
          <div><span className="section-kicker">SERVICIOS</span><h2 id="service-title">{isEdit ? 'Editar servicio' : 'Registrar servicio'}</h2></div>
          <button type="button" onClick={onClose} aria-label="Cerrar modal">×</button>
        </header>

        <form onSubmit={submit} className="appointment-form">
          <label><span>Nombre</span><input name="nombre" value={form.nombre} onChange={update} maxLength="150" required /></label>

          <label><span>Descripción</span><textarea name="descripcion" value={form.descripcion} onChange={update} rows="2" placeholder="Detalles del servicio..." /></label>

          <div className="modal-field-row">
            <label><span>Duración (minutos)</span><input type="number" name="duracionMinutos" min="1" max="1440" value={form.duracionMinutos} onChange={update} required /></label>
            <label><span>Precio (RD$)</span><input type="number" name="precio" min="0" step="0.01" value={form.precio} onChange={update} required /></label>
          </div>

          <div className="modal-field-row">
            <label><span>Buffer antes (min)</span><input type="number" name="minutosAntes" min="0" max="1440" value={form.minutosAntes} onChange={update} /></label>
            <label><span>Buffer después (min)</span><input type="number" name="minutosDespues" min="0" max="1440" value={form.minutosDespues} onChange={update} /></label>
          </div>

          <section className="modal-toggle-list">
            <div className="modal-toggle-row"><div><strong>Servicio activo</strong><small>{form.activo ? 'Disponible al crear citas.' : 'No aparecerá al crear citas.'}</small></div><button type="button" className={`toggle-switch ${form.activo ? 'on' : ''}`} onClick={toggleActive} aria-pressed={form.activo}><span /></button></div>
          </section>

          {error && <div className="modal-error" role="alert">{error?.message ?? error}</div>}

          <footer className="modal-actions">
            {isEdit && <button className="danger-button" type="button" onClick={removeService} disabled={saving}>Eliminar</button>}
            <span />
            <button className="secondary-button" type="button" onClick={onClose} disabled={saving}>Cancelar</button>
            <button className="save-button" type="submit" disabled={saving}>{saving ? 'Guardando...' : isEdit ? 'Guardar cambios' : 'Registrar servicio'}</button>
          </footer>
        </form>
      </section>
    </div>
  )
}
