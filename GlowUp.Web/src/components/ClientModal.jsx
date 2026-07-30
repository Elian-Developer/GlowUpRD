import { useState } from 'react'

const emptyClient = {
  nombre: '',
  apellido: '',
  telefono: '',
  correo: '',
  fechaNacimiento: '',
  genero: 'not_specified',
  notas: '',
}

const generos = [
  ['not_specified', 'Prefiere no decir'],
  ['female', 'Femenino'],
  ['male', 'Masculino'],
  ['other', 'Otro'],
]

export default function ClientModal({
  client,
  saving = false,
  error,
  onClose,
  onSave,
}) {
  const [form, setForm] = useState(() => ({ ...emptyClient, ...client }))
  const isEdit = Boolean(client?.id)

  function update(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function submit(event) {
    event.preventDefault()
    onSave(form)
  }

  return (
    <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className="appointment-modal" role="dialog" aria-modal="true" aria-labelledby="client-title">
        <header className="modal-header">
          <div><span className="section-kicker">CLIENTES</span><h2 id="client-title">{isEdit ? 'Editar cliente' : 'Nuevo cliente'}</h2></div>
          <button type="button" onClick={onClose} aria-label="Cerrar modal">×</button>
        </header>

        <form onSubmit={submit} className="appointment-form">
          <div className="modal-field-row">
            <label><span>Nombre</span><input name="nombre" value={form.nombre} onChange={update} maxLength="100" required /></label>
            <label><span>Apellido</span><input name="apellido" value={form.apellido} onChange={update} maxLength="100" required /></label>
          </div>

          <div className="modal-field-row">
            <label><span>Teléfono</span><input name="telefono" value={form.telefono} onChange={update} maxLength="30" /></label>
            <label><span>Correo</span><input type="email" name="correo" value={form.correo} onChange={update} maxLength="150" /></label>
          </div>

          <div className="modal-field-row">
            <label><span>Fecha de nacimiento</span><input type="date" name="fechaNacimiento" value={form.fechaNacimiento} onChange={update} /></label>
            <label>
              <span>Género</span>
              <select name="genero" value={form.genero} onChange={update}>
                {generos.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
          </div>

          <label><span>Notas</span><textarea name="notas" value={form.notas} onChange={update} rows="3" placeholder="Preferencias, alergias, historial..." /></label>

          {error && <div className="modal-error" role="alert">{error}</div>}

          <footer className="modal-actions">
            <span />
            <button className="secondary-button" type="button" onClick={onClose} disabled={saving}>Cancelar</button>
            <button className="save-button" type="submit" disabled={saving}>{saving ? 'Guardando...' : isEdit ? 'Guardar cambios' : 'Crear cliente'}</button>
          </footer>
        </form>
      </section>
    </div>
  )
}
