import { useEffect, useState } from 'react'
import { obtenerSucursal } from '../services/negociosApi'
import { obtenerEmpleado } from '../services/empleadosApi'

const emptyEmployee = {
  sucursalId: '',
  sucursalIds: [],
  nombre: '',
  apellido: '',
  telefono: '',
  correo: '',
  puesto: '',
  biografia: '',
  activo: true,
  crearAcceso: false,
  password: '',
  confirmPassword: '',
  servicioIds: [],
  horarios: [],
}

const days = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb']

export default function EmployeeModal({
  employee,
  branches,
  services = [],
  saving = false,
  error,
  onClose,
  onSave,
}) {
  const [form, setForm] = useState(() => {
    const initial = { ...emptyEmployee, ...employee }
    const defaultBranchId = initial.sucursalId ?? initial.sucursalIds?.[0] ?? branches[0]?.id ?? ''
    return { ...initial, horarios: (initial.horarios ?? []).map((shift) => ({ ...shift, sucursalId: shift.sucursalId ?? defaultBranchId })) }
  })
  const [scheduleBranchId, setScheduleBranchId] = useState(() => employee?.sucursalId ?? employee?.sucursalIds?.[0] ?? branches[0]?.id ?? '')
  const [branchSchedule, setBranchSchedule] = useState({ branchId: '', hours: [] })
  const [validationError, setValidationError] = useState('')
  const isEdit = Boolean(employee?.id)

  useEffect(() => {
    const inputs = document.querySelectorAll('.employee-modal input[type="time"]')
    const openPicker = (event) => {
      try { event.currentTarget.showPicker?.() } catch { /* The browser keeps its native time input behavior. */ }
    }
    inputs.forEach((input) => input.addEventListener('click', openPicker))
    return () => inputs.forEach((input) => input.removeEventListener('click', openPicker))
  }, [form.horarios])

  useEffect(() => {
    if (!form.negocioId || !scheduleBranchId) return undefined
    let active = true
    obtenerSucursal(form.negocioId, scheduleBranchId)
      .then((branch) => active && setBranchSchedule({ branchId: String(scheduleBranchId), hours: branch.horarios ?? [] }))
      .catch(() => active && setBranchSchedule({ branchId: String(scheduleBranchId), hours: [] }))
    return () => { active = false }
  }, [form.negocioId, scheduleBranchId])

  useEffect(() => {
    if (!employee?.id) return undefined
    let active = true
    obtenerEmpleado(employee.id).then((details) => {
      if (!active) return
      const sucursalIds = (details.sucursales ?? []).filter((branch) => branch.estado === 'active').map((branch) => String(branch.sucursalId))
      const horarios = (details.horarios ?? []).filter((shift) => shift.activo && shift.iniciaA && shift.terminaA).map((shift) => ({ ...shift, sucursalId: String(shift.sucursalId), iniciaA: shift.iniciaA.slice(0, 5), terminaA: shift.terminaA.slice(0, 5) }))
      setForm((current) => ({ ...current, sucursalId: details.sucursalId ? String(details.sucursalId) : current.sucursalId, sucursalIds, horarios }))
      setScheduleBranchId((current) => sucursalIds.includes(String(current)) ? String(current) : (sucursalIds[0] ?? ''))
    }).catch(() => { /* Se mantiene la información ya cargada si la actualización puntual falla. */ })
    return () => { active = false }
  }, [employee?.id])

  function update(event) {
    const { name, value, type, checked } = event.target
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }))
    setValidationError('')
  }

  function submit(event) {
    event.preventDefault()
    if (!isEdit && form.crearAcceso && form.password !== form.confirmPassword) {
      setValidationError('Las contraseñas no coinciden.')
      return
    }
    onSave(form)
  }

  function updateBranches(event) {
    const sucursalIds = Array.from(event.target.selectedOptions, (option) => option.value)
    setForm((current) => ({ ...current, sucursalIds, sucursalId: sucursalIds[0] ?? '' }))
    setScheduleBranchId((current) => sucursalIds.includes(String(current)) ? current : (sucursalIds[0] ?? ''))
  }

  function changeScheduleBranch(event) {
    const branchId = event.target.value
    setBranchSchedule({ branchId: '', hours: [] })
    setScheduleBranchId(branchId)
  }

  const assignedBranches = branches.filter((branch) => (form.sucursalIds ?? []).map(String).includes(String(branch.id)))

  function toggleService(serviceId) {
    const id = String(serviceId)
    setForm((current) => {
      const servicioIds = (current.servicioIds ?? []).map(String)
      return { ...current, servicioIds: servicioIds.includes(id) ? servicioIds.filter((item) => item !== id) : [...servicioIds, id] }
    })
  }

  function toggle(name) { setForm((current) => ({ ...current, [name]: !current[name] })) }
  function removeEmployee() {
    if (!window.confirm(`¿Eliminar a ${form.nombre} ${form.apellido}? Dejará de aparecer de Personal y de las nuevas citas, pero se conservará su historial.`)) return
    onSave({ ...form, eliminar: true })
  }

  function updateShift(index, key, value) {
    setForm((current) => ({ ...current, horarios: current.horarios.map((item, itemIndex) => itemIndex === index ? { ...item, [key]: value } : item) }))
  }
  function addShift(day) {
    if (branchSchedule.branchId !== String(scheduleBranchId) || branchSchedule.hours.some((schedule) => Number(schedule.diaSemana) === day && schedule.cerrado)) return
    setForm((current) => ({ ...current, horarios: [...current.horarios, { sucursalId: scheduleBranchId || current.sucursalIds?.[0], diaSemana: day, activo: true, iniciaA: '09:00', terminaA: '13:00' }] }))
  }
  function removeShift(index) { setForm((current) => ({ ...current, horarios: current.horarios.filter((_, itemIndex) => itemIndex !== index) })) }

  return (
    <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <section className="appointment-modal employee-modal" role="dialog" aria-modal="true" aria-labelledby="employee-title">
        <header className="modal-header">
          <div><span className="section-kicker">PERSONAL</span><h2 id="employee-title">{isEdit ? 'Editar empleado' : 'Registrar empleado'}</h2></div>
          <button type="button" onClick={onClose} aria-label="Cerrar modal">×</button>
        </header>

        <form onSubmit={submit} className="appointment-form">
          <div className="modal-field-row">
            <label><span>Nombre</span><input name="nombre" value={form.nombre} onChange={update} maxLength="100" required /></label>
            <label><span>Apellido</span><input name="apellido" value={form.apellido} onChange={update} maxLength="100" required /></label>
          </div>

          <label>
            <span>Sucursales asignadas</span>
            <select multiple value={form.sucursalIds ?? (form.sucursalId ? [form.sucursalId] : [])} onChange={updateBranches} required>
              {branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}
            </select>
            <small>Usa Ctrl o Cmd para asignar varias sedes.</small>
          </label>

          <div className="modal-field-row">
            <label><span>Teléfono</span><input name="telefono" value={form.telefono} onChange={update} maxLength="30" /></label>
            <label><span>Puesto</span><input name="puesto" value={form.puesto} onChange={update} placeholder="Ej. Estilista" maxLength="100" /></label>
          </div>

          <label><span>Correo</span><input type="email" name="correo" value={form.correo} onChange={update} maxLength="150" /></label>

          <label><span>Biografía</span><textarea name="biografia" value={form.biografia} onChange={update} rows="2" placeholder="Especialidades, experiencia..." /></label>

          {!isEdit && form.crearAcceso && (
            <><label>
              <span>Contraseña de acceso</span>
              <input type="password" name="password" value={form.password} onChange={update} minLength="8" placeholder="Mínimo 8 caracteres" required />
            </label><label>
              <span>Confirmar contraseña</span>
              <input type="password" name="confirmPassword" value={form.confirmPassword} onChange={update} minLength="8" placeholder="Repite la contraseña" required />
            </label></>
          )}

          <section className="employee-field-section">
            <span className="employee-field-label">Servicios que ofrece</span>
            <div className="employee-services">
              <small>Selecciona los servicios que este empleado puede atender.</small>
              {services.length > 0 ? <div className="employee-service-options">{services.map((service) => {
                const selected = (form.servicioIds ?? []).map(String).includes(String(service.id))
                return <button type="button" key={service.id} className={selected ? 'selected' : ''} onClick={() => toggleService(service.id)} aria-pressed={selected}>{service.name}</button>
              })}</div> : <p>No hay servicios activos para asignar.</p>}
              {(form.servicioIds ?? []).length === 0 && services.length > 0 && <em>Sin selección, podrá ofrecer todos los servicios activos.</em>}
            </div>
          </section>

          <section className="employee-field-section">
            <span className="employee-field-label">Horario de trabajo</span>
            <div className="employee-hours">{assignedBranches.length > 1 && <label className="employee-schedule-branch"><span>Sucursal para horario</span><select value={scheduleBranchId} onChange={changeScheduleBranch}>{assignedBranches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select></label>}<div className="employee-shifts" key={scheduleBranchId}>{days.map((day, dayIndex) => { const shifts = form.horarios.map((shift, index) => ({ shift, index })).filter(({ shift }) => shift.diaSemana === dayIndex && String(shift.sucursalId ?? form.sucursalId) === String(scheduleBranchId)).sort((left, right) => String(left.shift.iniciaA).localeCompare(String(right.shift.iniciaA))); const scheduleLoading = Boolean(form.negocioId && scheduleBranchId && branchSchedule.branchId !== String(scheduleBranchId)); const businessClosed = branchSchedule.hours.some((schedule) => Number(schedule.diaSemana) === dayIndex && schedule.cerrado); return <div className="employee-shift-day" key={day}><b>{day}</b><div>{shifts.map(({ shift, index }) => <span className="employee-shift" key={`${scheduleBranchId}-${index}`}><input type="time" value={shift.iniciaA} onChange={(event) => updateShift(index, 'iniciaA', event.target.value)} required /><i>—</i><input type="time" value={shift.terminaA} onChange={(event) => updateShift(index, 'terminaA', event.target.value)} required /><button type="button" onClick={() => removeShift(index)} aria-label={`Eliminar turno de ${day}`}>×</button></span>)}{!scheduleLoading && !businessClosed && <button type="button" className="row-action" onClick={() => addShift(dayIndex)}>+ Turno</button>}{shifts.length === 0 && <small>{scheduleLoading ? 'Cargando horario' : businessClosed ? 'Negocio cerrado' : 'No trabaja'}</small>}</div></div> })}</div></div>
          </section>

          <section className="modal-toggle-list">
            {!isEdit && <div className="modal-toggle-row"><div><strong>Acceso al panel</strong><small>Permite que el empleado inicie sesión.</small></div><button type="button" className={`toggle-switch ${form.crearAcceso ? 'on' : ''}`} onClick={() => toggle('crearAcceso')} aria-pressed={form.crearAcceso}><span /></button></div>}
            <div className="modal-toggle-row"><div><strong>Empleado activo</strong><small>{form.activo ? 'Disponible para asignar citas.' : 'No aparecerá al crear citas.'}</small></div><button type="button" className={`toggle-switch ${form.activo ? 'on' : ''}`} onClick={() => toggle('activo')} aria-pressed={form.activo}><span /></button></div>
          </section>

          {(validationError || error) && <div className="modal-error" role="alert">{validationError || error?.message || error}</div>}

          <footer className="modal-actions">
            {isEdit && <button className="danger-button" type="button" onClick={removeEmployee} disabled={saving}>Eliminar</button>}
            <span />
            <button className="secondary-button" type="button" onClick={onClose} disabled={saving}>Cancelar</button>
            <button className="save-button" type="submit" disabled={saving}>{saving ? 'Guardando...' : isEdit ? 'Guardar cambios' : 'Registrar empleado'}</button>
          </footer>
        </form>
      </section>
    </div>
  )
}
