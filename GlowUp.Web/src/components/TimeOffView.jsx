import { useCallback, useEffect, useState } from 'react'
import { actualizarAusencia, buscarAusencias, cancelarAusencia, crearAusencia } from '../services/ausenciasApi'

const typeLabels = { vacation: 'Vacaciones', permission: 'Permiso', absence: 'Ausencia' }
const todayKey = () => new Date().toLocaleDateString('en-CA')
const addDays = (key, days) => {
  const date = new Date(`${key}T12:00:00`)
  date.setDate(date.getDate() + days)
  return date.toLocaleDateString('en-CA')
}
const dateFromValue = (value) => value?.slice(0, 10) ?? ''
const timeFromValue = (value) => value?.slice(11, 16) ?? '09:00'
const isAllDay = (item) => timeFromValue(item.iniciaEn) === '00:00' && timeFromValue(item.terminaEn) === '00:00'

function TimeOffModal({ absence, employees, negocioId, saving, error, onClose, onSave }) {
  const initialAllDay = absence ? isAllDay(absence) : true
  const [form, setForm] = useState(() => ({
    empleadoId: absence?.empleadoId ? String(absence.empleadoId) : String(employees[0]?.id ?? ''),
    tipo: absence?.tipo ?? 'vacation',
    todoElDia: initialAllDay,
    fechaInicio: dateFromValue(absence?.iniciaEn) || todayKey(),
    fechaFin: absence && initialAllDay ? addDays(dateFromValue(absence.terminaEn), -1) : (dateFromValue(absence?.terminaEn) || todayKey()),
    horaInicio: timeFromValue(absence?.iniciaEn),
    horaFin: timeFromValue(absence?.terminaEn),
    motivo: absence?.motivo ?? '',
  }))

  const [validationError, setValidationError] = useState(null)

  useEffect(() => {
    const inputs = document.querySelectorAll('.time-off-modal input[type="date"], .time-off-modal input[type="time"]')
    const openPicker = (event) => {
      try { event.currentTarget.showPicker?.() } catch { /* The native control remains available. */ }
    }
    inputs.forEach((input) => input.addEventListener('click', openPicker))
    return () => inputs.forEach((input) => input.removeEventListener('click', openPicker))
  }, [])

  function update(event) {
    const { name, value } = event.target
    setValidationError(null)
    setForm((current) => {
      if (name === 'fechaInicio') return { ...current, fechaInicio: value, fechaFin: current.fechaFin < value ? value : current.fechaFin }
      if (name === 'fechaFin') return { ...current, fechaFin: value < current.fechaInicio ? current.fechaInicio : value }
      return { ...current, [name]: value }
    })
  }

  function toggleAllDay() { setValidationError(null); setForm((current) => ({ ...current, todoElDia: !current.todoElDia })) }
  function openDatePicker(event) { try { event.currentTarget.showPicker?.() } catch { /* Native picker remains available. */ } }

  function submit(event) {
    event.preventDefault()
    if (form.fechaFin < form.fechaInicio || (!form.todoElDia && form.fechaFin === form.fechaInicio && form.horaFin <= form.horaInicio)) {
      setValidationError('La fecha y hora de fin deben ser posteriores a las de inicio.')
      return
    }
    const iniciaEn = form.todoElDia ? `${form.fechaInicio}T00:00:00` : `${form.fechaInicio}T${form.horaInicio}:00`
    const terminaEn = form.todoElDia ? `${addDays(form.fechaFin, 1)}T00:00:00` : `${form.fechaFin}T${form.horaFin}:00`
    onSave({ negocioId: Number(negocioId), empleadoId: Number(form.empleadoId), tipo: form.tipo, iniciaEn, terminaEn, motivo: form.motivo.trim() || null })
  }

  return <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}><section className="appointment-modal time-off-modal" role="dialog" aria-modal="true" aria-labelledby="time-off-title"><header className="modal-header"><div><span className="section-kicker">PERSONAL</span><h2 id="time-off-title">{absence ? 'Editar ausencia' : 'Registrar ausencia'}</h2></div><button type="button" onClick={onClose} aria-label="Cerrar">×</button></header><form className="appointment-form" onSubmit={submit}><label><span>Empleado</span><select name="empleadoId" value={form.empleadoId} onChange={update} required>{employees.filter((employee) => employee.activo).map((employee) => <option key={employee.id} value={employee.id}>{employee.nombre} {employee.apellido}</option>)}</select></label><label><span>Tipo</span><select name="tipo" value={form.tipo} onChange={update}><option value="vacation">Vacaciones</option><option value="permission">Permiso</option><option value="absence">Ausencia</option></select></label><div className="modal-field-row"><label><span>Desde</span><input name="fechaInicio" type="date" value={form.fechaInicio} onClick={openDatePicker} onChange={update} required /></label><label><span>Hasta</span><input name="fechaFin" type="date" min={form.fechaInicio} value={form.fechaFin} onClick={openDatePicker} onChange={update} required /></label></div>{!form.todoElDia && <div className="modal-field-row"><label><span>Hora de inicio</span><input name="horaInicio" type="time" value={form.horaInicio} onClick={openDatePicker} onChange={update} required /></label><label><span>Hora de fin</span><input name="horaFin" type="time" value={form.horaFin} onClick={openDatePicker} onChange={update} required /></label></div>}<label><span>Motivo</span><textarea name="motivo" value={form.motivo} onChange={update} rows="3" maxLength="255" placeholder="Opcional" /></label><div className="modal-toggle-row"><div><strong>Todo el día</strong><small>Bloquea la agenda durante todo el rango.</small></div><button type="button" className={`toggle-switch ${form.todoElDia ? 'on' : ''}`} onClick={toggleAllDay} aria-pressed={form.todoElDia} aria-label="Cambiar todo el día"><span /></button></div>{(validationError || error) && <div className="modal-error" role="alert">{validationError || error}</div>}<footer className="modal-actions"><span /><button className="secondary-button" type="button" onClick={onClose} disabled={saving}>Cancelar</button><button className="save-button" type="submit" disabled={saving}>{saving ? 'Guardando...' : absence ? 'Guardar cambios' : 'Registrar ausencia'}</button></footer></form></section></div>
}

export default function TimeOffView({ negocioId, employees, onUpdated }) {
  const [desde, setDesde] = useState(todayKey)
  const [hasta, setHasta] = useState(() => addDays(todayKey(), 90))
  const [empleadoId, setEmpleadoId] = useState('')
  const [tipo, setTipo] = useState('all')
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [modal, setModal] = useState(null)
  const [saving, setSaving] = useState(false)
  const [modalError, setModalError] = useState(null)

  useEffect(() => {
    const inputs = document.querySelectorAll('.time-off-filters input[type="date"]')
    const openPicker = (event) => {
      try { event.currentTarget.showPicker?.() } catch { /* The native control remains available. */ }
    }
    inputs.forEach((input) => input.addEventListener('click', openPicker))
    return () => inputs.forEach((input) => input.removeEventListener('click', openPicker))
  }, [])

  const load = useCallback(async () => {
    if (!negocioId || !desde || !hasta) return
    try { setItems(await buscarAusencias({ negocioId, desde, hasta, empleadoId: empleadoId || undefined, incluirCanceladas: false })); setError(null) } catch (err) { setError(err.message) } finally { setLoading(false) }
  }, [negocioId, desde, hasta, empleadoId])

  useEffect(() => {
    let active = true
    buscarAusencias({ negocioId, desde, hasta, empleadoId: empleadoId || undefined, incluirCanceladas: false })
      .then((nextItems) => { if (active) { setItems(nextItems); setError(null) } })
      .catch((err) => active && setError(err.message))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [negocioId, desde, hasta, empleadoId])
  const filtered = items.filter((item) => item.estado === 'scheduled' && (tipo === 'all' || item.tipo === tipo))

  async function save(payload) {
    setSaving(true); setModalError(null)
    try {
      if (modal?.id) await actualizarAusencia(modal.id, payload)
      else await crearAusencia(payload)
      setModal(null); await load(); onUpdated?.()
    } catch (err) { setModalError(err.message) } finally { setSaving(false) }
  }

  async function cancel(item) {
    if (!window.confirm(`¿Cancelar ${typeLabels[item.tipo].toLowerCase()} de ${item.empleado}?`)) return
    try { await cancelarAusencia(item.id); await load(); onUpdated?.() } catch (err) { setError(err.message) }
  }

  return <section className="data-card time-off-view"><div className="card-heading time-off-heading"><div><span className="section-kicker">DISPONIBILIDAD</span><h2>Ausencias del personal</h2><p>Registra los períodos en que un empleado no estará disponible.</p></div><button className="new-appointment" onClick={() => { setModal({}); setModalError(null) }}><svg className="ui-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 5v14M5 12h14" /></svg>Registrar ausencia</button></div><div className="data-toolbar time-off-filters"><select value={empleadoId} onChange={(event) => setEmpleadoId(event.target.value)}><option value="">Todo el personal</option>{employees.map((employee) => <option key={employee.id} value={employee.id}>{employee.nombre} {employee.apellido}</option>)}</select><select value={tipo} onChange={(event) => setTipo(event.target.value)}><option value="all">Todos los tipos</option><option value="vacation">Vacaciones</option><option value="permission">Permisos</option><option value="absence">Ausencias</option></select><input className="date-filter time-off-date-filter" type="date" value={desde} onChange={(event) => setDesde(event.target.value)} aria-label="Fecha desde" /><span>—</span><input className="date-filter time-off-date-filter" type="date" min={desde} value={hasta} onChange={(event) => setHasta(event.target.value)} aria-label="Fecha hasta" /></div>{error && <div className="modal-error">{error}</div>}{loading ? <div className="empty-state">Cargando ausencias...</div> : <div className="responsive-table"><table><thead><tr><th>Empleado</th><th>Tipo</th><th>Periodo</th><th>Motivo</th><th>Estado</th><th /></tr></thead><tbody>{filtered.map((item) => <tr key={item.id}><td>{item.empleado}</td><td><span className={`time-off-type ${item.tipo}`}>{typeLabels[item.tipo]}</span></td><td>{dateFromValue(item.iniciaEn)} {isAllDay(item) ? `— ${addDays(dateFromValue(item.terminaEn), -1)}` : `${timeFromValue(item.iniciaEn)} — ${dateFromValue(item.terminaEn)} ${timeFromValue(item.terminaEn)}`}</td><td>{item.motivo || '—'}</td><td><span className={`table-status ${item.estado === 'scheduled' ? 'pending' : 'cancelled'}`}>{item.estado === 'scheduled' ? 'Programada' : 'Cancelada'}</span></td><td>{item.estado === 'scheduled' && <><button className="row-action" onClick={() => { setModal(item); setModalError(null) }}>Editar</button><button className="row-action danger-text" onClick={() => cancel(item)}>Cancelar</button></>}</td></tr>)}</tbody></table>{filtered.length === 0 && <div className="empty-state">No hay ausencias en este período.</div>}</div>}{modal && <TimeOffModal absence={modal.id ? modal : null} employees={employees} negocioId={negocioId} saving={saving} error={modalError} onClose={() => setModal(null)} onSave={save} />}</section>
}
