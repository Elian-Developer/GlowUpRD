import { useState } from 'react'

const emptyEmployee = {
  sucursalId: '',
  nombre: '',
  apellido: '',
  telefono: '',
  correo: '',
  puesto: '',
  biografia: '',
  activo: true,
  crearAcceso: false,
  password: '',
}

export default function EmployeeModal({
  employee,
  branches,
  saving = false,
  error,
  onClose,
  onSave,
}) {
  const [form, setForm] = useState(() => ({ ...emptyEmployee, ...employee }))
  const isEdit = Boolean(employee?.id)

  function update(event) {
    const { name, value, type, checked } = event.target
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }))
  }

  function submit(event) {
    event.preventDefault()
    onSave(form)
  }

  return (
    <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className="appointment-modal" role="dialog" aria-modal="true" aria-labelledby="employee-title">
        <header className="modal-header">
          <div><span className="section-kicker">PERSONAL</span><h2 id="employee-title">{isEdit ? 'Editar empleado' : 'Nuevo empleado'}</h2></div>
          <button type="button" onClick={onClose} aria-label="Cerrar modal">×</button>
        </header>

        <form onSubmit={submit} className="appointment-form">
          <div className="modal-field-row">
            <label><span>Nombre</span><input name="nombre" value={form.nombre} onChange={update} maxLength="100" required /></label>
            <label><span>Apellido</span><input name="apellido" value={form.apellido} onChange={update} maxLength="100" required /></label>
          </div>

          <label>
            <span>Sucursal</span>
            <select name="sucursalId" value={form.sucursalId} onChange={update}>
              <option value="">Sin asignar</option>
              {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
            </select>
          </label>

          <div className="modal-field-row">
            <label><span>Teléfono</span><input name="telefono" value={form.telefono} onChange={update} maxLength="30" /></label>
            <label><span>Puesto</span><input name="puesto" value={form.puesto} onChange={update} placeholder="Ej. Estilista" maxLength="100" /></label>
          </div>

          <label><span>Correo</span><input type="email" name="correo" value={form.correo} onChange={update} maxLength="150" /></label>

          <label><span>Biografía</span><textarea name="biografia" value={form.biografia} onChange={update} rows="2" placeholder="Especialidades, experiencia..." /></label>

          {!isEdit && (
            <label className="checkbox">
              <input type="checkbox" name="crearAcceso" checked={form.crearAcceso} onChange={update} />
              <span />Necesita acceso al panel (podrá iniciar sesión)
            </label>
          )}

          {!isEdit && form.crearAcceso && (
            <label>
              <span>Contraseña de acceso</span>
              <input type="password" name="password" value={form.password} onChange={update} minLength="8" placeholder="Mínimo 8 caracteres" required />
            </label>
          )}

          <label className="checkbox"><input type="checkbox" name="activo" checked={form.activo} onChange={update} /><span />Activo</label>

          {error && <div className="modal-error" role="alert">{error}</div>}

          <footer className="modal-actions">
            <span />
            <button className="secondary-button" type="button" onClick={onClose} disabled={saving}>Cancelar</button>
            <button className="save-button" type="submit" disabled={saving}>{saving ? 'Guardando...' : isEdit ? 'Guardar cambios' : 'Crear empleado'}</button>
          </footer>
        </form>
      </section>
    </div>
  )
}
