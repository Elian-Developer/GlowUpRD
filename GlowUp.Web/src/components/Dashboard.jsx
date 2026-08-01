import { useCallback, useEffect, useMemo, useState } from 'react'
import glowUpLogo from '../assets/glowup-rd-logo.png'
import useLocalStorage from '../hooks/useLocalStorage'
import {
  actualizarCita,
  buscarCitas,
  crearCita,
  eliminarCita,
  obtenerCatalogos,
  obtenerNegocios,
} from '../services/citasApi'
import { actualizarEmpleado, buscarEmpleados, crearEmpleado } from '../services/empleadosApi'
import { actualizarCliente, buscarClientes, crearCliente } from '../services/clientesApi'
import { actualizarServicio, buscarServicios, crearServicio } from '../services/serviciosApi'
import { obtenerReporte } from '../services/reportesApi'
import { actualizarNegocio, obtenerNegocio } from '../services/negociosApi'
import AppointmentModal from './AppointmentModal'
import EmployeeModal from './EmployeeModal'
import ClientModal from './ClientModal'
import ServiceModal from './ServiceModal'
import './Dashboard.css'

const navigation = [
  ['dashboard', 'Resumen'], ['calendar', 'Calendario'], ['appointments', 'Citas'],
  ['customers', 'Clientes'], ['services', 'Servicios'], ['team', 'Personal'],
  ['reports', 'Reportes'], ['settings', 'Configuración'],
]

const tones = ['mint', 'blue', 'sand', 'violet']
const sectionTitles = Object.fromEntries(navigation)
const CALENDAR_SLOT_MINUTES = 15

function Icon({ name }) {
  const paths = {
    dashboard: <><rect x="3" y="3" width="7" height="7" rx="2" /><rect x="14" y="3" width="7" height="7" rx="2" /><rect x="3" y="14" width="7" height="7" rx="2" /><rect x="14" y="14" width="7" height="7" rx="2" /></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="3" /><path d="M8 3v4M16 3v4M3 10h18M8 14h3M8 17h6" /></>,
    appointments: <><rect x="4" y="3" width="16" height="18" rx="3" /><path d="M8 2v4M16 2v4M8 11h8M8 15h5" /></>,
    customers: <><circle cx="9" cy="8" r="4" /><path d="M2.5 21c.5-4.5 3-7 6.5-7s6 2.5 6.5 7M16 5.5a3.5 3.5 0 010 7M17 15c2.5.5 4 2.5 4.5 5" /></>,
    services: <><path d="M6 3l12 18M18 3L6 21" /><circle cx="6" cy="4" r="2" /><circle cx="18" cy="4" r="2" /></>,
    team: <><circle cx="12" cy="7" r="4" /><path d="M4 21c.6-5 3.5-8 8-8s7.4 3 8 8" /></>,
    reports: <><path d="M4 20V10M10 20V4M16 20v-7M22 20H2" /></>,
    settings: <><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.7 1.7 0 00.3 1.9l.1.1-2.8 2.8-.1-.1a1.7 1.7 0 00-1.9-.3A1.7 1.7 0 0014 21v.2h-4V21a1.7 1.7 0 00-1-1.6 1.7 1.7 0 00-1.9.3l-.1.1L4.2 17l.1-.1A1.7 1.7 0 004.6 15 1.7 1.7 0 003 14H2.8v-4H3a1.7 1.7 0 001.6-1 1.7 1.7 0 00-.3-1.9L4.2 7 7 4.2l.1.1A1.7 1.7 0 009 4.6 1.7 1.7 0 0010 3v-.2h4V3a1.7 1.7 0 001 1.6 1.7 1.7 0 001.9-.3l.1-.1L19.8 7l-.1.1a1.7 1.7 0 00-.3 1.9 1.7 1.7 0 001.6 1h.2v4H21a1.7 1.7 0 00-1.6 1z" /></>,
    plus: <path d="M12 5v14M5 12h14" />,
    bell: <><path d="M18 8a6 6 0 00-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" /><path d="M10 21h4" /></>,
    menu: <path d="M4 7h16M4 12h16M4 17h16" />,
    chevron: <path d="M9 18l6-6-6-6" />,
    logout: <><path d="M10 17l5-5-5-5M15 12H3" /><path d="M14 3h5a2 2 0 012 2v14a2 2 0 01-2 2h-5" /></>,
    search: <><circle cx="11" cy="11" r="7" /><path d="M20 20l-4-4" /></>,
  }
  return <svg className="ui-icon" viewBox="0 0 24 24" aria-hidden="true">{paths[name]}</svg>
}

function toDateKey(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function formatLongDate(date) {
  return new Intl.DateTimeFormat('es-DO', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(date)
}

function addDays(date, amount) {
  const next = new Date(date)
  next.setDate(next.getDate() + amount)
  return next
}

function getInitials(name) {
  return name.split(' ').filter(Boolean).map((part) => part[0]).slice(0, 2).join('').toUpperCase()
}

function statusLabel(status) {
  return {
    pending: 'Pendiente',
    confirmed: 'Confirmada',
    completed: 'Completada',
    cancelled: 'Cancelada',
    no_show: 'No asistió',
  }[status] ?? status
}

function toTime(value) {
  return value?.slice(11, 16) ?? '09:00'
}

function mapCatalog(catalog) {
  return {
    branches: (catalog?.sucursales ?? []).map((item) => ({ id: String(item.id), name: item.nombre, detail: item.detalle })),
    customers: (catalog?.clientes ?? []).map((item) => ({ id: String(item.id), name: item.nombre, phone: item.detalle, email: item.detalle, initials: getInitials(item.nombre ?? '') })),
    professionals: (catalog?.empleados ?? []).map((item, index) => ({
      id: String(item.id),
      name: item.nombre,
      shortName: item.nombre?.split(' ')[0] ?? item.nombre,
      initials: getInitials(item.nombre ?? ''),
      specialty: item.detalle ?? 'Profesional',
      tone: tones[index % tones.length],
    })),
    services: (catalog?.servicios ?? []).map((item) => ({
      id: String(item.id),
      name: item.nombre,
      duration: item.duracionMinutos,
      price: item.precio,
      bufferBefore: item.minutosAntes ?? 0,
      bufferAfter: item.minutosDespues ?? 0,
      category: 'Servicio',
    })),
  }
}

function mapAppointment(item) {
  const service = item.servicios?.[0]
  return {
    id: String(item.id),
    businessId: String(item.negocioId),
    branchId: String(item.sucursalId),
    branchName: item.sucursal,
    customerId: String(item.clienteId),
    customerName: item.cliente,
    professionalId: String(item.empleadoId),
    professionalName: item.empleado,
    serviceId: service ? String(service.servicioId) : '',
    serviceName: service?.nombre,
    serviceIds: (item.servicios ?? []).map((detail) => String(detail.servicioId)),
    date: item.fecha,
    time: toTime(item.inicio),
    startsAt: item.inicio,
    duration: (item.servicios ?? []).reduce((sum, detail) => sum + detail.duracionMinutos, 0),
    bufferBefore: (item.servicios ?? []).reduce((sum, detail) => sum + (detail.minutosAntes ?? 0), 0),
    bufferAfter: (item.servicios ?? []).reduce((sum, detail) => sum + (detail.minutosDespues ?? 0), 0),
    status: item.estado,
    notes: item.notas ?? '',
    cancellationReason: item.motivoCancelacion,
    total: item.total,
  }
}

function buildAppointmentPayload(form, businessId) {
  return {
    negocioId: Number(businessId),
    sucursalId: Number(form.branchId),
    clienteId: Number(form.customerId),
    empleadoId: Number(form.professionalId),
    inicio: `${form.date}T${form.time.length === 5 ? `${form.time}:00` : form.time}`,
    servicioIds: [Number(form.serviceId)],
    estado: form.status,
    motivoCancelacion: form.status === 'cancelled' ? 'Cancelada desde el panel' : null,
    notas: form.notes?.trim() || null,
  }
}

function mapEmployee(item) {
  return {
    id: String(item.id),
    negocioId: String(item.negocioId),
    sucursalId: item.sucursalId ? String(item.sucursalId) : '',
    sucursal: item.sucursal,
    nombre: item.nombre,
    apellido: item.apellido,
    telefono: item.telefono ?? '',
    correo: item.correo ?? '',
    puesto: item.puesto ?? '',
    biografia: item.biografia ?? '',
    estado: item.estado,
    activo: item.estado === 'active',
    tieneAcceso: item.tieneAcceso,
  }
}

function buildEmpleadoPayload(form, businessId) {
  return {
    negocioId: Number(businessId),
    sucursalId: form.sucursalId ? Number(form.sucursalId) : null,
    nombre: form.nombre.trim(),
    apellido: form.apellido.trim(),
    telefono: form.telefono?.trim() || null,
    correo: form.correo?.trim() || null,
    puesto: form.puesto?.trim() || null,
    biografia: form.biografia?.trim() || null,
    activo: form.activo,
    crearAcceso: Boolean(form.crearAcceso),
    password: form.crearAcceso ? form.password : null,
  }
}

function mapClient(item) {
  return {
    id: String(item.id),
    nombre: item.nombre,
    apellido: item.apellido,
    telefono: item.telefono ?? '',
    correo: item.correo ?? '',
    fechaNacimiento: item.fechaNacimiento ?? '',
    genero: item.genero ?? 'not_specified',
    notas: item.notas ?? '',
    estado: item.estado,
    activo: item.estado === 'active',
    totalVisitas: item.totalVisitas ?? 0,
  }
}

function buildClientePayload(form, businessId) {
  return {
    negocioId: Number(businessId),
    nombre: form.nombre.trim(),
    apellido: form.apellido.trim(),
    telefono: form.telefono?.trim() || null,
    correo: form.correo?.trim() || null,
    fechaNacimiento: form.fechaNacimiento || null,
    genero: form.genero || null,
    notas: form.notas?.trim() || null,
  }
}

function mapServiceItem(item) {
  return {
    id: String(item.id),
    negocioId: String(item.negocioId),
    nombre: item.nombre,
    descripcion: item.descripcion ?? '',
    duracionMinutos: item.duracionMinutos,
    precio: item.precio,
    minutosAntes: item.minutosAntes,
    minutosDespues: item.minutosDespues,
    activo: item.activo,
  }
}

function buildServicioPayload(form, businessId) {
  return {
    negocioId: Number(businessId),
    categoriaId: null,
    nombre: form.nombre.trim(),
    descripcion: form.descripcion?.trim() || null,
    duracionMinutos: Number(form.duracionMinutos),
    precio: Number(form.precio),
    minutosAntes: Number(form.minutosAntes) || 0,
    minutosDespues: Number(form.minutosDespues) || 0,
    activo: form.activo,
  }
}

function Schedule({ selectedDate, appointments, professionals, customers, services, horarios, onChangeDate, onOpenAppointment, expanded = false }) {
  const [uiSettings] = useLocalStorage('glowup_ui_settings', { reminders: true, confirmations: true, compactCalendar: false })
  const dayAppointments = appointments.filter((item) => item.date === toDateKey(selectedDate) && item.status !== 'cancelled')
  const schedule = getDaySchedule(horarios, selectedDate)
  const timeSlots = getTimeSlots(schedule)
  const rowHeight = uiSettings.compactCalendar ? (expanded ? 40 : 37) : (expanded ? 53 : 45)
  const gridStyle = {
    gridTemplateColumns: `58px repeat(${Math.max(professionals.length, 1)}, minmax(150px, 1fr))`,
    gridTemplateRows: `49px repeat(${timeSlots.length}, ${rowHeight}px)`,
  }

  function openSlot(professionalId, hour) {
    onOpenAppointment({ date: toDateKey(selectedDate), time: hour, professionalId })
  }

  return (
    <article className={`schedule-card ${expanded ? 'schedule-expanded' : ''} ${uiSettings.compactCalendar ? 'compact-calendar' : ''}`}>
      <div className="card-heading schedule-heading">
        <div><span className="section-kicker">AGENDA</span><h2>{expanded ? 'Calendario de citas' : 'Calendario de hoy'}</h2><p>{formatLongDate(selectedDate)}</p></div>
        <div className="calendar-controls"><button onClick={() => onChangeDate(addDays(selectedDate, -1))} aria-label="Día anterior">‹</button><button className="today-button" onClick={() => onChangeDate(new Date())}>Hoy</button><button onClick={() => onChangeDate(addDays(selectedDate, 1))} aria-label="Día siguiente">›</button></div>
      </div>
      {professionals.length === 0 ? <div className="empty-state">No hay profesionales activos para este negocio.</div> : !schedule ? <div className="calendar-closed"><strong>El negocio está cerrado este día.</strong><p>Elige otra fecha para consultar o crear una cita.</p></div> : (
        <div className="calendar-scroll">
          <div className="calendar-board" style={gridStyle}>
            <div className="calendar-corner">Hora</div>
            {professionals.map((professional) => <div className="professional-heading" key={professional.id}><span className={`professional-avatar ${professional.tone}`}>{professional.initials}</span>{professional.shortName}</div>)}
            {timeSlots.map((hour, index) => <div className="time-label" key={hour} style={{ gridRow: index + 2 }}>{hour}</div>)}
            {timeSlots.flatMap((hour, row) => professionals.map((professional, column) => <button aria-label={`Crear cita con ${professional.name} a las ${hour}`} className="calendar-cell" key={`${professional.id}-${hour}`} style={{ gridColumn: column + 2, gridRow: row + 2 }} onClick={() => openSlot(professional.id, hour)} />))}
            {dayAppointments.map((appointment) => {
              const professionalIndex = professionals.findIndex((item) => item.id === appointment.professionalId)
              const appointmentMinutes = timeToMinutes(appointment.time)
              const blockedStart = appointmentMinutes - appointment.bufferBefore
              const row = Math.max(2, Math.floor((blockedStart - schedule.opensAt) / CALENDAR_SLOT_MINUTES) + 2)
              const span = Math.max(1, Math.ceil((appointment.duration + appointment.bufferBefore + appointment.bufferAfter) / CALENDAR_SLOT_MINUTES))
              const customer = customers.find((item) => item.id === appointment.customerId)
              const service = services.find((item) => item.id === appointment.serviceId)
              if (professionalIndex < 0 || appointmentMinutes === null || row > timeSlots.length + 1) return null
              return <button className={`appointment-event ${appointment.status}`} key={appointment.id} style={{ gridColumn: professionalIndex + 2, gridRow: `${row} / span ${span}` }} onClick={() => onOpenAppointment(appointment)}><strong>{service?.name ?? appointment.serviceName ?? 'Servicio'}</strong><span>{appointment.time}</span><small>{customer?.name ?? appointment.customerName ?? 'Cliente'}{appointment.bufferBefore || appointment.bufferAfter ? ` · Buffer ${appointment.bufferBefore + appointment.bufferAfter} min` : ''}</small></button>
            })}
          </div>
        </div>
      )}
      <footer className="calendar-legend"><span><i className="confirmed" />Confirmada</span><span><i className="pending" />Pendiente</span><span><i className="completed" />Completada</span><span><i className="cancelled" />Cancelada</span><span><i className="no_show" />No asistió</span><small>Selecciona un espacio libre para crear una cita</small></footer>
    </article>
  )
}

function AppointmentList({ appointments, customers, professionals, services, selectedDate, onChangeDate, onEdit, onNew }) {
  const [status, setStatus] = useState('all')
  const [query, setQuery] = useState('')

  function openDatePicker(event) {
    try {
      event.currentTarget.showPicker?.()
    } catch {
      // Some browsers open the native date picker through their default input behavior.
    }
  }

  const filtered = appointments
    .filter((item) => item.date === selectedDate)
    .filter((item) => status === 'all' || item.status === status)
    .filter((item) => (customers.find((customer) => customer.id === item.customerId)?.name ?? item.customerName ?? '').toLowerCase().includes(query.toLowerCase()))
    .sort((a, b) => `${b.date}${b.time}`.localeCompare(`${a.date}${a.time}`))

  return <section className="data-card"><div className="data-toolbar"><div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar cliente..." /></div><input className="date-filter" type="date" value={selectedDate} onClick={openDatePicker} onChange={(event) => onChangeDate(event.target.value)} aria-label="Filtrar citas por fecha" /><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="all">Todos los estados</option><option value="confirmed">Confirmadas</option><option value="pending">Pendientes</option><option value="completed">Completadas</option><option value="cancelled">Canceladas</option><option value="no_show">No asistió</option></select><button className="new-appointment" onClick={onNew}><Icon name="plus" />Nueva cita</button></div><div className="responsive-table"><table><thead><tr><th>Fecha y hora</th><th>Cliente</th><th>Servicio</th><th>Profesional</th><th>Estado</th><th /></tr></thead><tbody>{filtered.map((appointment) => <tr key={appointment.id}><td><strong>{appointment.date}</strong><small>{appointment.time}</small></td><td>{customers.find((item) => item.id === appointment.customerId)?.name ?? appointment.customerName}</td><td>{services.find((item) => item.id === appointment.serviceId)?.name ?? appointment.serviceName}</td><td>{professionals.find((item) => item.id === appointment.professionalId)?.name ?? appointment.professionalName}</td><td><span className={`table-status ${appointment.status}`}>{statusLabel(appointment.status)}</span></td><td><button className="row-action" onClick={() => onEdit(appointment)}>Editar</button></td></tr>)}</tbody></table>{filtered.length === 0 && <div className="empty-state">No hay citas que coincidan con los filtros.</div>}</div></section>
}

function ServicesView({ services, onEdit, onNew }) {
  const [query, setQuery] = useState('')
  const filtered = services.filter((item) => item.nombre.toLowerCase().includes(query.toLowerCase()))
  return (
    <section className="data-card">
      <div className="data-toolbar">
        <div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar servicio..." /></div>
        <button className="new-appointment" onClick={onNew}><Icon name="plus" />Nuevo servicio</button>
      </div>
      <div className="directory-grid">
        {filtered.map((item) => (
          <button type="button" key={item.id} className="directory-card directory-card-button" onClick={() => onEdit(item)}>
            <span className="directory-avatar">{getInitials(item.nombre)}</span>
            <div>
              <strong>{item.nombre}</strong>
              <p>RD$ {Number(item.precio).toLocaleString()} · {item.duracionMinutos} min</p>
              <small>{item.activo ? 'Activo' : 'Inactivo'}</small>
            </div>
          </button>
        ))}
      </div>
      {filtered.length === 0 && <div className="empty-state">No hay servicios que coincidan.</div>}
    </section>
  )
}

function TeamView({ employees, onEdit, onNew }) {
  const [query, setQuery] = useState('')
  const filtered = employees.filter((item) => `${item.nombre} ${item.apellido}`.toLowerCase().includes(query.toLowerCase()))
  return (
    <section className="data-card">
      <div className="data-toolbar">
        <div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar empleado..." /></div>
        <button className="new-appointment" onClick={onNew}><Icon name="plus" />Nuevo empleado</button>
      </div>
      <div className="directory-grid">
        {filtered.map((item) => (
          <button type="button" key={item.id} className="directory-card directory-card-button" onClick={() => onEdit(item)}>
            <span className="directory-avatar">{getInitials(`${item.nombre} ${item.apellido}`)}</span>
            <div>
              <strong>{item.nombre} {item.apellido}</strong>
              <p>{item.puesto || 'Sin puesto asignado'}</p>
              <small>{item.sucursal ?? 'Sin sucursal'} · {item.activo ? 'Activo' : 'Inactivo'}{item.tieneAcceso ? ' · Con acceso al panel' : ''}</small>
            </div>
          </button>
        ))}
      </div>
      {filtered.length === 0 && <div className="empty-state">No hay empleados que coincidan.</div>}
    </section>
  )
}

function ClientsView({ clients, onEdit, onNew }) {
  const [query, setQuery] = useState('')
  const filtered = clients.filter((item) => `${item.nombre} ${item.apellido}`.toLowerCase().includes(query.toLowerCase()))
  return (
    <section className="data-card">
      <div className="data-toolbar">
        <div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar cliente..." /></div>
        <button className="new-appointment" onClick={onNew}><Icon name="plus" />Nuevo cliente</button>
      </div>
      <div className="directory-grid">
        {filtered.map((item) => (
          <button type="button" key={item.id} className="directory-card directory-card-button" onClick={() => onEdit(item)}>
            <span className="directory-avatar">{getInitials(`${item.nombre} ${item.apellido}`)}</span>
            <div>
              <strong>{item.nombre} {item.apellido}</strong>
              <p>{item.telefono || item.correo || 'Sin contacto'}</p>
              <small>{item.activo ? 'Activo' : 'Inactivo'} · {item.totalVisitas} visitas</small>
            </div>
          </button>
        ))}
      </div>
      {filtered.length === 0 && <div className="empty-state">No hay clientes que coincidan.</div>}
    </section>
  )
}

const diaLabels = { 1: 'Lun', 2: 'Mar', 3: 'Mié', 4: 'Jue', 5: 'Vie', 6: 'Sáb', 0: 'Dom' }
const diaOrden = [1, 2, 3, 4, 5, 6, 0]

function formatMoney(value) {
  return `RD$ ${Number(value ?? 0).toLocaleString('es-DO', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`
}

function timeToMinutes(value) {
  if (!value) return null
  const [hour, minute] = value.slice(0, 5).split(':').map(Number)
  return Number.isFinite(hour) && Number.isFinite(minute) ? hour * 60 + minute : null
}

function minutesToTime(minutes) {
  return `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`
}

function getDaySchedule(horarios, date) {
  const schedule = horarios?.find((item) => Number(item.diaSemana) === date.getDay())
  const opensAt = timeToMinutes(schedule?.abreA)
  const closesAt = timeToMinutes(schedule?.cierraA)
  if (!schedule || schedule.cerrado || opensAt === null || closesAt === null || closesAt <= opensAt) return null
  return { opensAt, closesAt }
}

function getTimeSlots(schedule) {
  if (!schedule) return []
  return Array.from({ length: Math.ceil((schedule.closesAt - schedule.opensAt) / CALENDAR_SLOT_MINUTES) }, (_, index) => minutesToTime(schedule.opensAt + index * CALENDAR_SLOT_MINUTES))
}

function findNextOpenSlot(horarios, date) {
  for (let offset = 0; offset < 14; offset += 1) {
    const candidate = addDays(date, offset)
    const schedule = getDaySchedule(horarios, candidate)
    if (schedule) return { date: candidate, time: minutesToTime(schedule.opensAt) }
  }
  return null
}

function ReportLineChart({ data }) {
  const values = data.map((item) => Number(item.ingresos ?? 0))
  const max = Math.max(...values, 1)
  const width = 640
  const height = 210
  const points = values.map((value, index) => `${(index / Math.max(values.length - 1, 1)) * width},${height - (value / max) * 170 - 18}`).join(' ')
  return <div className="report-line-chart"><svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Evolución diaria de ingresos"><path className="report-chart-grid" d={`M0 40H${width}M0 105H${width}M0 170H${width}`} /><polyline className="report-chart-line" points={points} />{values.map((value, index) => { const [x, y] = points.split(' ')[index].split(','); return <circle key={data[index].fecha} className="report-chart-dot" cx={x} cy={y} r="4" /> })}</svg><div className="report-chart-labels">{data.map((item) => <small key={item.fecha}>{new Intl.DateTimeFormat('es-DO', { day: 'numeric', month: 'short' }).format(new Date(`${item.fecha}T12:00:00`))}</small>)}</div></div>
}

function AgendaHealthMetric({ title, value, description, tone }) {
  const circumference = 251.2
  const offset = circumference - Math.min(100, value) / 100 * circumference
  return <div className="agenda-health-metric"><div className={`agenda-ring ${tone}`}><svg viewBox="0 0 100 100" role="img" aria-label={`${title}: ${value}%`}><circle className="agenda-ring-track" cx="50" cy="50" r="40" /><circle className="agenda-ring-value" cx="50" cy="50" r="40" style={{ strokeDasharray: circumference, strokeDashoffset: offset }} /></svg><strong>{value}%</strong></div><div><strong>{title}</strong><p>{description}</p></div></div>
}

function ReportData({ negocioId, days }) {
  const [reporte, setReporte] = useState(null)
  const [loadingReporte, setLoadingReporte] = useState(true)
  const [reporteError, setReporteError] = useState(null)

  useEffect(() => {
    if (!negocioId) return
    let active = true
    const hasta = new Date()
    const desde = addDays(hasta, -(days - 1))
    obtenerReporte({ negocioId, desde: toDateKey(desde), hasta: toDateKey(hasta) })
      .then((data) => { if (active) setReporte(data) })
      .catch((err) => active && setReporteError(err.message))
      .finally(() => active && setLoadingReporte(false))
    return () => { active = false }
  }, [negocioId, days])

  if (loadingReporte) return <section className="empty-panel">Cargando reportes...</section>
  if (reporteError) return <section className="empty-panel error-panel"><h2>No pudimos cargar los reportes.</h2><p>{reporteError}</p></section>
  if (!reporte) return null

  const serviceTotal = reporte.rendimientoServicios.reduce((sum, item) => sum + Number(item.ingresos ?? 0), 0)
  const serviceGradient = reporte.rendimientoServicios.length
    ? `conic-gradient(${reporte.rendimientoServicios.map((item, index) => { const colors = ['#2b858c', '#527c96', '#d19b53', '#735c9a', '#789c69']; const start = reporte.rendimientoServicios.slice(0, index).reduce((sum, current) => sum + Number(current.ingresos ?? 0), 0) / Math.max(serviceTotal, 1) * 100; const end = start + Number(item.ingresos ?? 0) / Math.max(serviceTotal, 1) * 100; return `${colors[index % colors.length]} ${start}% ${end}%` }).join(', ')})`
    : 'conic-gradient(#e5eaed 0 100%)'
  const maxEmployeeIncome = Math.max(...reporte.rendimientoEmpleados.map((item) => Number(item.ingresos ?? 0)), 1)
  const metrics = [
    { title: 'Ingresos realizados', value: formatMoney(reporte.ingresosTotales), note: 'Citas confirmadas y completadas', icon: 'reports', tone: 'soft' },
    { title: 'Tasa de retención', value: `${reporte.tasaRetencion}%`, note: 'Clientes que regresaron', icon: 'team', tone: 'green' },
    { title: 'Clientes nuevos', value: reporte.clientesNuevos, note: 'Añadidos en este período', icon: 'customers', tone: 'dark' },
    { title: 'Ticket promedio', value: formatMoney(reporte.ticketPromedio), note: `${reporte.serviciosAgendados} citas con ingreso`, icon: 'calendar', tone: 'amber' },
  ]

  return <><section className="stats-grid report-kpis">{metrics.map((metric) => <article className="stat-card" key={metric.title}><span className={`stat-icon ${metric.tone}`}><Icon name={metric.icon} /></span><div><small>{metric.title}</small><strong>{metric.value}</strong><p>{metric.note}</p></div></article>)}</section><section className="reports-grid"><article className="chart-card report-trend-card"><div className="report-card-heading"><div><span className="section-kicker">EVOLUCIÓN</span><h2>Ingresos realizados por día</h2></div><strong>{formatMoney(reporte.ingresosTotales)}</strong></div><ReportLineChart data={reporte.evolucionDiaria} /></article><article className="chart-card report-health-card"><div><span className="section-kicker">SALUD DE AGENDA</span><h2>Reservas y cancelaciones</h2></div><div className="agenda-health-list"><AgendaHealthMetric title="Citas no confirmadas" value={reporte.tasaNoConfirmadas ?? 0} description={`${reporte.citasPendientes} de ${reporte.citasTotales} reservas permanecen pendientes.`} tone="teal" /><AgendaHealthMetric title="Tasa de cancelación" value={reporte.tasaCancelacion} description={`${reporte.citasCanceladas} de ${reporte.citasTotales} citas del período.`} tone="coral" /></div></article><article className="chart-card report-staff-card"><div><span className="section-kicker">PERSONAL</span><h2>Rendimiento por ingresos</h2></div><div className="report-staff-list">{reporte.rendimientoEmpleados.map((item) => <div key={item.empleadoId}><div><strong>{item.nombre}</strong><small>{item.citas} citas · {formatMoney(item.ingresos)}</small><small className="employee-occupancy">Ocupación de agenda: {item.ocupacionAgenda ?? 0}%</small></div><span><i style={{ width: `${Number(item.ingresos ?? 0) / maxEmployeeIncome * 100}%` }} /></span></div>)}{reporte.rendimientoEmpleados.length === 0 && <p>Aún no hay ingresos por empleado.</p>}</div></article><article className="chart-card report-services-card"><div><span className="section-kicker">SERVICIOS</span><h2>Ingresos realizados por servicio</h2></div><div className="report-donut-wrap"><div className="report-donut" style={{ background: serviceGradient }}><span>{reporte.rendimientoServicios.length}<small>servicios</small></span></div><div className="report-service-legend">{reporte.rendimientoServicios.map((item, index) => <div key={item.nombre}><i className={`service-tone-${index % 5}`} /><span>{item.nombre}</span><b>{formatMoney(item.ingresos)}</b><small>{item.cantidad} citas con ingreso</small></div>)}{reporte.rendimientoServicios.length === 0 && <p>Sin servicios con ingresos realizados.</p>}</div></div></article></section></>
}

function ReportsView({ negocioId }) {
  const [days, setDays] = useState(30)
  return <section className="reports-view"><div className="report-toolbar"><div><span className="section-kicker">ANÁLISIS DEL NEGOCIO</span><p>Ingresos realizados: citas confirmadas y completadas.</p></div><div className="period-selector" aria-label="Período de reportes">{[7, 30, 60].map((option) => <button type="button" key={option} className={days === option ? 'active' : ''} onClick={() => setDays(option)}>Últimos {option} días</button>)}</div></div><ReportData key={`${negocioId}-${days}`} negocioId={negocioId} days={days} /></section>
}

function BusinessSettingsView({ negocioId, settings, setSettings, onBusinessUpdated }) {
  const [form, setForm] = useState(null)
  const [loadingProfile, setLoadingProfile] = useState(true)
  const [savingProfile, setSavingProfile] = useState(false)
  const [profileError, setProfileError] = useState(null)
  const [profileMessage, setProfileMessage] = useState(null)

  useEffect(() => {
    let active = true
    obtenerNegocio(negocioId).then((data) => {
      if (!active) return
      const byDay = Object.fromEntries((data.horarios ?? []).map((item) => [item.diaSemana, item]))
      setForm({ ...data, sucursalPrincipal: data.sucursalPrincipal, horarios: diaOrden.map((dia) => ({ diaSemana: dia, abreA: byDay[dia]?.abreA?.slice(0, 5) ?? '09:00', cierraA: byDay[dia]?.cierraA?.slice(0, 5) ?? '18:00', cerrado: byDay[dia]?.cerrado ?? false })) })
    }).catch((err) => active && setProfileError(err.message)).finally(() => active && setLoadingProfile(false))
    return () => { active = false }
  }, [negocioId])

  function toggle(key) { setSettings((current) => ({ ...current, [key]: !current[key] })) }
  const options = [['reminders', 'Recordatorios automáticos', 'Enviar recordatorios antes de cada cita.'], ['confirmations', 'Confirmación de citas', 'Solicitar confirmación al cliente.'], ['compactCalendar', 'Calendario compacto', 'Reducir la altura de los bloques del calendario.']]
  function update(path, value) { setForm((current) => path === 'sucursal' ? { ...current, sucursalPrincipal: { ...current.sucursalPrincipal, [value.name]: value.value } } : { ...current, [path]: value }) }
  function updateSchedule(day, key, value) { setForm((current) => ({ ...current, horarios: current.horarios.map((item) => item.diaSemana === day ? { ...item, [key]: value } : item) })) }
  async function save() {
    setSavingProfile(true); setProfileError(null); setProfileMessage(null)
    try {
      const horarios = form.horarios.map((item) => ({
        ...item,
        abreA: item.cerrado || !item.abreA ? null : `${item.abreA}:00`,
        cierraA: item.cerrado || !item.cierraA ? null : `${item.cierraA}:00`,
      }))
      const updated = await actualizarNegocio(negocioId, { nombre: form.nombre, rnc: form.rnc || null, telefono: form.telefono || null, correo: form.correo || null, descripcion: form.descripcion || null, logoUrl: form.logoUrl || null, sucursalPrincipal: form.sucursalPrincipal, horarios })
      setForm((current) => ({ ...current, ...updated, sucursalPrincipal: updated.sucursalPrincipal, horarios: updated.horarios.map((item) => ({ ...item, abreA: item.abreA?.slice(0, 5) ?? '09:00', cierraA: item.cierraA?.slice(0, 5) ?? '18:00' })) }))
      onBusinessUpdated(updated.horarios ?? [])
      setProfileMessage('Los cambios se guardaron en tu negocio.')
    } catch (err) { setProfileError(err.message) } finally { setSavingProfile(false) }
  }
  if (loadingProfile) return <section className="empty-panel">Cargando perfil del negocio...</section>
  if (profileError && !form) return <section className="empty-panel error-panel"><h2>No pudimos cargar el perfil.</h2><p>{profileError}</p></section>
  return <section className="business-settings"><div className="settings-page-heading"><div><span className="section-kicker">GESTIÓN DEL NEGOCIO</span><h2>Perfil y operación</h2><p>Actualiza la información visible de tu negocio, su sucursal y horarios.</p></div><button type="button" className="save-button" onClick={save} disabled={savingProfile}>{savingProfile ? 'Guardando...' : 'Guardar cambios'}</button></div>{profileMessage && <p className="profile-feedback success">{profileMessage}</p>}{profileError && <p className="profile-feedback error">{profileError}</p>}<div className="business-settings-grid"><article className="settings-card business-profile-card"><div className="settings-heading"><h2>Perfil del negocio</h2><p>Información que identifica tu negocio.</p></div><div className="settings-form"><label>Nombre<input value={form.nombre ?? ''} onChange={(event) => update('nombre', event.target.value)} /></label><label>Tipo de negocio<input value={form.tipoNegocio ?? ''} readOnly /></label><label>RNC<input value={form.rnc ?? ''} onChange={(event) => update('rnc', event.target.value)} placeholder="Opcional" /></label><label>Teléfono<input value={form.telefono ?? ''} onChange={(event) => update('telefono', event.target.value)} /></label><label>Correo<input type="email" value={form.correo ?? ''} onChange={(event) => update('correo', event.target.value)} /></label><label className="settings-field-full">Descripción<textarea value={form.descripcion ?? ''} onChange={(event) => update('descripcion', event.target.value)} rows="4" placeholder="Cuéntales a tus clientes sobre tu negocio." /></label><label className="settings-field-full">URL del logo<input type="url" value={form.logoUrl ?? ''} onChange={(event) => update('logoUrl', event.target.value)} placeholder="https://..." /></label>{form.logoUrl && <div className="logo-preview"><img src={form.logoUrl} alt="Vista previa del logo" onError={(event) => { event.currentTarget.style.display = 'none' }} /></div>}</div></article><article className="settings-card"><div className="settings-heading"><h2>Sucursal principal</h2><p>Dirección y contacto de tu ubicación principal.</p></div><div className="settings-form">{[['nombre', 'Nombre de sucursal'], ['telefono', 'Teléfono'], ['direccion', 'Dirección'], ['ciudad', 'Ciudad'], ['provincia', 'Provincia'], ['pais', 'País']].map(([key, label]) => <label key={key}>{label}<input value={form.sucursalPrincipal?.[key] ?? ''} onChange={(event) => update('sucursal', { name: key, value: event.target.value })} /></label>)}</div></article><article className="settings-card schedule-settings-card"><div className="settings-heading"><h2>Horarios de atención</h2><p>Define cuándo está disponible tu sucursal principal.</p></div><div className="schedule-settings-list">{form.horarios.map((item) => <div key={item.diaSemana}><strong>{diaLabels[item.diaSemana]}</strong><button type="button" className={`toggle-switch ${!item.cerrado ? 'on' : ''}`} onClick={() => updateSchedule(item.diaSemana, 'cerrado', !item.cerrado)} aria-label={`Cambiar disponibilidad de ${diaLabels[item.diaSemana]}`}><span /></button>{item.cerrado ? <em>Cerrado</em> : <><input type="time" value={item.abreA} onChange={(event) => updateSchedule(item.diaSemana, 'abreA', event.target.value)} /><b>—</b><input type="time" value={item.cierraA} onChange={(event) => updateSchedule(item.diaSemana, 'cierraA', event.target.value)} /></>}</div>)}</div></article><article className="settings-card local-preferences-card"><div className="settings-heading"><h2>Preferencias de interfaz</h2><p>Estos ajustes se guardan solamente en este navegador.</p></div>{options.map(([key, title, description]) => <div className="setting-row" key={key}><div><strong>{title}</strong><p>{description}</p></div><button className={`toggle-switch ${settings[key] ? 'on' : ''}`} onClick={() => toggle(key)} aria-pressed={settings[key]}><span /></button></div>)}</article></div></section>
}

function LogoutConfirmation({ onCancel, onConfirm }) {
  return (
    <div className="modal-layer" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onCancel()}>
      <section className="logout-modal" role="dialog" aria-modal="true" aria-labelledby="logout-title" aria-describedby="logout-description">
        <span className="logout-modal-icon"><Icon name="logout" /></span>
        <h2 id="logout-title">¿Cerrar sesión?</h2>
        <p id="logout-description">Tendrás que ingresar nuevamente para acceder a tu cuenta.</p>
        <div className="logout-modal-actions">
          <button className="secondary-button" type="button" onClick={onCancel}>Cancelar</button>
          <button className="logout-confirm-button" type="button" onClick={onConfirm} autoFocus>Cerrar sesión</button>
        </div>
      </section>
    </div>
  )
}

function Dashboard({ session, onLogout }) {
  const [activeSection, setActiveSection] = useState('dashboard')
  const [menuOpen, setMenuOpen] = useState(false)
  const [selectedDate, setSelectedDate] = useState(new Date())
  const [appointmentListDate, setAppointmentListDate] = useState(() => toDateKey(new Date()))
  const [businesses, setBusinesses] = useState([])
  const [selectedBusinessId, setSelectedBusinessId] = useState('')
  const [branches, setBranches] = useState([])
  const [customers, setCustomers] = useState([])
  const [professionals, setProfessionals] = useState([])
  const [services, setServices] = useState([])
  const [businessHours, setBusinessHours] = useState([])
  const [appointments, setAppointments] = useState([])
  const [teamMembers, setTeamMembers] = useState([])
  const [clientsList, setClientsList] = useState([])
  const [serviceList, setServiceList] = useState([])
  const [loading, setLoading] = useState(true)
  const [appointmentsLoading, setAppointmentsLoading] = useState(false)
  const [error, setError] = useState(null)
  const [modalError, setModalError] = useState(null)
  const [savingAppointment, setSavingAppointment] = useState(false)
  const [settings, setSettings] = useLocalStorage('glowup_ui_settings', { reminders: true, confirmations: true, compactCalendar: false })
  const [modalAppointment, setModalAppointment] = useState(null)
  const [modalEmployee, setModalEmployee] = useState(null)
  const [savingEmployee, setSavingEmployee] = useState(false)
  const [employeeModalError, setEmployeeModalError] = useState(null)
  const [modalClient, setModalClient] = useState(null)
  const [savingClient, setSavingClient] = useState(false)
  const [clientModalError, setClientModalError] = useState(null)
  const [modalService, setModalService] = useState(null)
  const [savingService, setSavingService] = useState(false)
  const [serviceModalError, setServiceModalError] = useState(null)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [logoutConfirmationOpen, setLogoutConfirmationOpen] = useState(false)
  const user = session.usuario
  const initials = `${user.nombre?.[0] ?? ''}${user.apellido?.[0] ?? ''}`.toUpperCase()
  const dateKey = toDateKey(selectedDate)
  const dayAppointments = useMemo(() => appointments.filter((item) => item.date === dateKey && item.status !== 'cancelled'), [appointments, dateKey])
  const daySchedule = getDaySchedule(businessHours, selectedDate)
  const availableMinutes = daySchedule ? (daySchedule.closesAt - daySchedule.opensAt) * professionals.length : 0
  const occupiedMinutes = dayAppointments
    .filter((item) => item.status !== 'no_show')
    .reduce((sum, item) => sum + item.duration + item.bufferBefore + item.bufferAfter, 0)
  const dayOccupancy = availableMinutes > 0 ? Math.min(100, Math.round(occupiedMinutes / availableMinutes * 100)) : 0
  const revenue = dayAppointments.reduce((sum, item) => sum + (item.total ?? services.find((service) => service.id === item.serviceId)?.price ?? 0), 0)
  const replaceAppointmentsForDate = useCallback((date, nextAppointments) => {
    setAppointments((current) => [
      ...current.filter((item) => item.businessId === selectedBusinessId && item.date !== date),
      ...nextAppointments,
    ])
  }, [selectedBusinessId])

  useEffect(() => {
    let active = true
    obtenerNegocios()
      .then((items) => {
        if (!active) return
        setBusinesses(items ?? [])
        setSelectedBusinessId(items?.[0]?.id ? String(items[0].id) : '')
      })
      .catch((err) => active && setError(err.message))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!selectedBusinessId) {
      return
    }

    let active = true

    Promise.all([
      obtenerCatalogos(selectedBusinessId),
      buscarCitas({ negocioId: selectedBusinessId, desde: dateKey, hasta: dateKey }),
      buscarEmpleados({ negocioId: selectedBusinessId, incluirInactivos: true }),
      buscarClientes({ negocioId: selectedBusinessId, incluirInactivos: true }),
      buscarServicios({ negocioId: selectedBusinessId, incluirInactivos: true }),
      obtenerNegocio(selectedBusinessId),
    ])
      .then(([catalog, citas, empleados, clientes, servicios, negocio]) => {
        if (!active) return
        const mappedCatalog = mapCatalog(catalog)
        setBranches(mappedCatalog.branches)
        setCustomers(mappedCatalog.customers)
        setProfessionals(mappedCatalog.professionals)
        setServices(mappedCatalog.services)
        replaceAppointmentsForDate(dateKey, (citas ?? []).map(mapAppointment))
        setTeamMembers((empleados ?? []).map(mapEmployee))
        setClientsList((clientes ?? []).map(mapClient))
        setServiceList((servicios ?? []).map(mapServiceItem))
        setBusinessHours(negocio?.horarios ?? [])
      })
      .catch((err) => active && setError(err.message))
      .finally(() => active && setAppointmentsLoading(false))

    return () => { active = false }
  }, [selectedBusinessId, dateKey, replaceAppointmentsForDate])

  useEffect(() => {
    if (activeSection !== 'appointments' || !selectedBusinessId) return

    let active = true
    buscarCitas({ negocioId: selectedBusinessId, desde: appointmentListDate, hasta: appointmentListDate })
      .then((citas) => active && replaceAppointmentsForDate(appointmentListDate, (citas ?? []).map(mapAppointment)))
      .catch((err) => active && setError(err.message))

    return () => { active = false }
  }, [activeSection, appointmentListDate, selectedBusinessId, replaceAppointmentsForDate])

  async function refreshTeam() {
    if (!selectedBusinessId) return
    const empleados = await buscarEmpleados({ negocioId: selectedBusinessId, incluirInactivos: true })
    setTeamMembers((empleados ?? []).map(mapEmployee))
  }

  async function refreshClients() {
    if (!selectedBusinessId) return
    const clientes = await buscarClientes({ negocioId: selectedBusinessId, incluirInactivos: true })
    setClientsList((clientes ?? []).map(mapClient))
  }

  async function refreshServices() {
    if (!selectedBusinessId) return
    const servicios = await buscarServicios({ negocioId: selectedBusinessId, incluirInactivos: true })
    setServiceList((servicios ?? []).map(mapServiceItem))
  }

  async function refreshAppointmentCatalog() {
    if (!selectedBusinessId) return
    const catalog = await obtenerCatalogos(selectedBusinessId)
    const mappedCatalog = mapCatalog(catalog)
    setBranches(mappedCatalog.branches)
    setCustomers(mappedCatalog.customers)
    setProfessionals(mappedCatalog.professionals)
    setServices(mappedCatalog.services)
  }

  function openNewService() {
    setServiceModalError(null)
    setModalService({ activo: true })
  }

  async function saveService(form) {
    setSavingService(true)
    setServiceModalError(null)
    try {
      const payload = buildServicioPayload(form, selectedBusinessId)
      if (form.id) {
        await actualizarServicio(form.id, payload)
      } else {
        await crearServicio(payload)
      }
      setModalService(null)
      await refreshServices()
      await refreshAppointmentCatalog()
    } catch (err) {
      setServiceModalError(err.message)
    } finally {
      setSavingService(false)
    }
  }

  function openNewClient() {
    setClientModalError(null)
    setModalClient({})
  }

  async function saveClient(form) {
    setSavingClient(true)
    setClientModalError(null)
    try {
      const payload = buildClientePayload(form, selectedBusinessId)
      if (form.id) {
        await actualizarCliente(form.id, payload)
      } else {
        await crearCliente(payload)
      }
      setModalClient(null)
      await refreshClients()
      await refreshAppointmentCatalog()
    } catch (err) {
      setClientModalError(err.message)
    } finally {
      setSavingClient(false)
    }
  }

  function openNewEmployee() {
    setEmployeeModalError(null)
    setModalEmployee({ activo: true, crearAcceso: false })
  }

  async function saveEmployee(form) {
    setSavingEmployee(true)
    setEmployeeModalError(null)
    try {
      const payload = buildEmpleadoPayload(form, selectedBusinessId)
      if (form.id) {
        await actualizarEmpleado(form.id, payload)
      } else {
        await crearEmpleado(payload)
      }
      setModalEmployee(null)
      await refreshTeam()
      await refreshAppointmentCatalog()
    } catch (err) {
      setEmployeeModalError(err.message)
    } finally {
      setSavingEmployee(false)
    }
  }

  async function refreshAppointments(date = dateKey) {
    if (!selectedBusinessId) return
    const citas = await buscarCitas({ negocioId: selectedBusinessId, desde: date, hasta: date })
    replaceAppointmentsForDate(date, (citas ?? []).map(mapAppointment))
  }

  function selectSection(section) { setActiveSection(section); setMenuOpen(false) }

  function openNewAppointment(preset = {}) {
    const requestedDate = preset.date ? new Date(`${preset.date}T12:00:00`) : selectedDate
    const schedule = getDaySchedule(businessHours, requestedDate)
    const nextSlot = schedule
      ? { date: requestedDate, time: minutesToTime(schedule.opensAt) }
      : findNextOpenSlot(businessHours, requestedDate)
    if (!nextSlot) {
      setError('No hay horarios de atención disponibles para crear una cita.')
      return
    }

    setModalError(null)
    setModalAppointment({
      branchId: branches[0]?.id ?? '',
      duration: services[0]?.duration ?? 60,
      bufferBefore: services[0]?.bufferBefore ?? 0,
      bufferAfter: services[0]?.bufferAfter ?? 0,
      status: 'confirmed',
      notes: '',
      customerId: customers[0]?.id ?? '',
      serviceId: services[0]?.id ?? '',
      professionalId: professionals[0]?.id ?? '',
      ...preset,
      date: schedule ? (preset.date ?? toDateKey(nextSlot.date)) : toDateKey(nextSlot.date),
      time: preset.time ?? nextSlot.time,
    })
  }

  async function saveAppointment(form) {
    setSavingAppointment(true)
    setModalError(null)
    try {
      const payload = buildAppointmentPayload(form, selectedBusinessId)
      const previousDate = modalAppointment?.date
      if (form.id) {
        await actualizarCita(form.id, payload)
      } else {
        await crearCita(payload)
      }
      const appointmentDate = form.date
      setSelectedDate(new Date(`${appointmentDate}T12:00:00`))
      setModalAppointment(null)
      await refreshAppointments(appointmentDate)
      if (previousDate && previousDate !== appointmentDate) {
        await refreshAppointments(previousDate)
      }
    } catch (err) {
      setModalError(err.message)
    } finally {
      setSavingAppointment(false)
    }
  }

  async function deleteAppointment(id) {
    setSavingAppointment(true)
    setModalError(null)
    try {
      const appointmentDate = modalAppointment?.date ?? dateKey
      await eliminarCita(id)
      setModalAppointment(null)
      await refreshAppointments(appointmentDate)
    } catch (err) {
      setModalError(err.message)
    } finally {
      setSavingAppointment(false)
    }
  }

  function renderMainContent() {
    if (loading) return <section className="empty-panel">Cargando negocios...</section>
    if (businesses.length === 0) return <section className="empty-panel"><h2>No tienes negocios activos todavía.</h2><p>Cuando exista un negocio asociado a tu usuario, aquí aparecerán sus citas, servicios y clientes.</p></section>
    if (error) return <section className="empty-panel error-panel"><h2>No pudimos cargar el dashboard.</h2><p>{error}</p></section>
    if (activeSection === 'calendar') return <Schedule expanded selectedDate={selectedDate} appointments={appointments} professionals={professionals} customers={customers} services={services} horarios={businessHours} onChangeDate={setSelectedDate} onOpenAppointment={openNewAppointment} />
    if (activeSection === 'appointments') return <AppointmentList appointments={appointments} customers={customers} professionals={professionals} services={services} selectedDate={appointmentListDate} onChangeDate={setAppointmentListDate} onEdit={setModalAppointment} onNew={() => openNewAppointment({ date: appointmentListDate })} />
    if (activeSection === 'team') return <TeamView employees={teamMembers} onEdit={setModalEmployee} onNew={openNewEmployee} />
    if (activeSection === 'customers') return <ClientsView clients={clientsList} onEdit={setModalClient} onNew={openNewClient} />
    if (activeSection === 'services') return <ServicesView services={serviceList} onEdit={setModalService} onNew={openNewService} />
    if (activeSection === 'reports') return <ReportsView key={selectedBusinessId} negocioId={selectedBusinessId} />
    if (activeSection === 'settings') return <BusinessSettingsView key={selectedBusinessId} negocioId={selectedBusinessId} settings={settings} setSettings={setSettings} onBusinessUpdated={setBusinessHours} />
    return <><section className="stats-grid"><article className="stat-card"><span className="stat-icon green"><Icon name="calendar" /></span><div><small>Citas del día</small><strong>{dayAppointments.length}</strong><p><b>{dayAppointments.filter((item) => item.status === 'confirmed').length}</b> confirmadas</p></div></article><article className="stat-card"><span className="stat-icon dark"><Icon name="customers" /></span><div><small>Clientes agendados</small><strong>{new Set(dayAppointments.map((item) => item.customerId)).size}</strong><p>Agenda seleccionada</p></div></article><article className="stat-card"><span className="stat-icon soft"><Icon name="reports" /></span><div><small>Ingresos estimados</small><strong>RD$ {revenue.toLocaleString()}</strong><p>Servicios no cancelados</p></div></article><article className="stat-card"><span className="stat-icon amber"><Icon name="team" /></span><div><small>Personal activo</small><strong>{professionals.length}</strong><p>Profesionales del negocio</p></div></article></section><section className="workspace-grid"><Schedule selectedDate={selectedDate} appointments={appointments} professionals={professionals} customers={customers} services={services} horarios={businessHours} onChangeDate={setSelectedDate} onOpenAppointment={openNewAppointment} /><aside className="today-card"><div className="card-heading"><div><span className="section-kicker">PRÓXIMAS</span><h2>Citas por atender</h2></div></div>{appointmentsLoading && <div className="empty-state compact">Actualizando agenda...</div>}{!appointmentsLoading && dayAppointments.sort((a, b) => a.time.localeCompare(b.time)).slice(0, 5).map((appointment) => <button className="next-appointment" key={appointment.id} onClick={() => setModalAppointment(appointment)}><span className="appointment-time">{appointment.time}</span><div><strong>{customers.find((item) => item.id === appointment.customerId)?.name ?? appointment.customerName}</strong><p>{services.find((item) => item.id === appointment.serviceId)?.name ?? appointment.serviceName} · {professionals.find((item) => item.id === appointment.professionalId)?.shortName ?? appointment.professionalName}</p></div><Icon name="chevron" /></button>)}{!appointmentsLoading && dayAppointments.length === 0 && <div className="empty-state compact">No hay citas para este día.</div>}<button className="view-all-button" onClick={() => selectSection('appointments')}>Ver todas las citas <Icon name="chevron" /></button><div className="occupancy-card"><div><span>Ocupación del día</span><strong>{dayOccupancy}%</strong></div><div className="progress-track"><span style={{ width: `${dayOccupancy}%` }} /></div><p>{availableMinutes > 0 ? `${occupiedMinutes} de ${availableMinutes} min ocupados` : 'Sin capacidad disponible este día'}</p></div></aside></section></>
  }

  return <main className="dashboard-page">{menuOpen && <button className="sidebar-backdrop" aria-label="Cerrar menú" onClick={() => setMenuOpen(false)} />}<aside className={`dashboard-sidebar ${menuOpen ? 'open' : ''}`}><div className="sidebar-logo-wrap"><img src={glowUpLogo} alt="GlowUp RD" className="sidebar-logo" /></div><nav className="sidebar-nav" aria-label="Navegación principal"><span className="nav-label">GESTIÓN</span>{navigation.slice(0, 6).map(([key, label]) => <button key={key} className={activeSection === key ? 'active' : ''} onClick={() => selectSection(key)}><Icon name={key} /><span>{label}</span></button>)}<span className="nav-label nav-label-secondary">ANÁLISIS</span>{navigation.slice(6).map(([key, label]) => <button key={key} className={activeSection === key ? 'active' : ''} onClick={() => selectSection(key)}><Icon name={key} /><span>{label}</span></button>)}</nav><div className="sidebar-user"><span className="user-avatar">{initials || 'GU'}</span><span className="user-info"><strong>{user.nombre} {user.apellido}</strong><small>{user.correo}</small></span><button onClick={() => setLogoutConfirmationOpen(true)} title="Cerrar sesión" aria-label="Cerrar sesión"><Icon name="logout" /></button></div></aside><section className="dashboard-content"><header className="dashboard-header"><div className="header-title-wrap"><button className="mobile-menu-button" onClick={() => setMenuOpen(true)} aria-label="Abrir menú"><Icon name="menu" /></button><div><span className="page-kicker">{activeSection === 'dashboard' ? 'PANEL PRINCIPAL' : 'GESTIÓN'}</span><h1>{activeSection === 'dashboard' ? `Buenos días, ${user.nombre}` : sectionTitles[activeSection]}</h1><p className="current-date">{formatLongDate(new Date())}</p></div></div><div className="header-actions">{businesses.length > 1 && <select className="business-selector" value={selectedBusinessId} onChange={(event) => setSelectedBusinessId(event.target.value)}>{businesses.map((business) => <option key={business.id} value={business.id}>{business.nombre}</option>)}</select>}<div className="notification-wrap"><button className="notification-button" aria-label="Notificaciones" onClick={() => setNotificationsOpen((open) => !open)}><Icon name="bell" /><span /></button>{notificationsOpen && <div className="notification-popover"><strong>Notificaciones</strong><p>Tienes {dayAppointments.filter((item) => item.status === 'pending').length} citas pendientes de confirmar.</p><button onClick={() => { selectSection('appointments'); setNotificationsOpen(false) }}>Revisar citas</button></div>}</div><button className="new-appointment" onClick={() => openNewAppointment()} disabled={!selectedBusinessId || customers.length === 0 || professionals.length === 0 || services.length === 0}><Icon name="plus" />Nueva cita</button></div></header>{renderMainContent()}</section>{modalAppointment && <AppointmentModal appointment={modalAppointment} branches={branches} customers={customers} professionals={professionals} services={services} horarios={businessHours} negocioId={selectedBusinessId} saving={savingAppointment} error={modalError} onClose={() => setModalAppointment(null)} onSave={saveAppointment} onDelete={deleteAppointment} />}{modalEmployee && <EmployeeModal employee={modalEmployee} branches={branches} saving={savingEmployee} error={employeeModalError} onClose={() => setModalEmployee(null)} onSave={saveEmployee} />}{modalClient && <ClientModal client={modalClient} saving={savingClient} error={clientModalError} onClose={() => setModalClient(null)} onSave={saveClient} />}{modalService && <ServiceModal service={modalService} saving={savingService} error={serviceModalError} onClose={() => setModalService(null)} onSave={saveService} />}{logoutConfirmationOpen && <LogoutConfirmation onCancel={() => setLogoutConfirmationOpen(false)} onConfirm={onLogout} />}</main>
}

export default Dashboard
