import { useEffect, useMemo, useState } from 'react'
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

const hours = ['08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00']
const tones = ['mint', 'blue', 'sand', 'violet']
const sectionTitles = Object.fromEntries(navigation)

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

function Schedule({ selectedDate, appointments, professionals, customers, services, onChangeDate, onOpenAppointment, expanded = false }) {
  const [uiSettings] = useLocalStorage('glowup_ui_settings', { reminders: true, confirmations: true, compactCalendar: false })
  const dayAppointments = appointments.filter((item) => item.date === toDateKey(selectedDate) && item.status !== 'cancelled')
  const gridStyle = { gridTemplateColumns: `58px repeat(${Math.max(professionals.length, 1)}, minmax(150px, 1fr))` }

  function openSlot(professionalId, hour) {
    onOpenAppointment({ date: toDateKey(selectedDate), time: hour, professionalId })
  }

  return (
    <article className={`schedule-card ${expanded ? 'schedule-expanded' : ''} ${uiSettings.compactCalendar ? 'compact-calendar' : ''}`}>
      <div className="card-heading schedule-heading">
        <div><span className="section-kicker">AGENDA</span><h2>{expanded ? 'Calendario de citas' : 'Calendario de hoy'}</h2><p>{formatLongDate(selectedDate)}</p></div>
        <div className="calendar-controls"><button onClick={() => onChangeDate(addDays(selectedDate, -1))} aria-label="Día anterior">‹</button><button className="today-button" onClick={() => onChangeDate(new Date())}>Hoy</button><button onClick={() => onChangeDate(addDays(selectedDate, 1))} aria-label="Día siguiente">›</button></div>
      </div>
      {professionals.length === 0 ? <div className="empty-state">No hay profesionales activos para este negocio.</div> : (
        <div className="calendar-scroll">
          <div className="calendar-board" style={gridStyle}>
            <div className="calendar-corner">Hora</div>
            {professionals.map((professional) => <div className="professional-heading" key={professional.id}><span className={`professional-avatar ${professional.tone}`}>{professional.initials}</span>{professional.shortName}</div>)}
            {hours.map((hour, index) => <div className="time-label" key={hour} style={{ gridRow: index + 2 }}>{hour}</div>)}
            {hours.flatMap((hour, row) => professionals.map((professional, column) => <button aria-label={`Crear cita con ${professional.name} a las ${hour}`} className="calendar-cell" key={`${professional.id}-${hour}`} style={{ gridColumn: column + 2, gridRow: row + 2 }} onClick={() => openSlot(professional.id, hour)} />))}
            {dayAppointments.map((appointment) => {
              const professionalIndex = professionals.findIndex((item) => item.id === appointment.professionalId)
              const [hour, minute] = appointment.time.split(':').map(Number)
              const row = Math.max(2, hour - 8 + 2)
              const span = Math.max(1, Math.ceil(appointment.duration / 60))
              const customer = customers.find((item) => item.id === appointment.customerId)
              const service = services.find((item) => item.id === appointment.serviceId)
              if (professionalIndex < 0 || row > 11) return null
              return <button className={`appointment-event ${appointment.status}`} key={appointment.id} style={{ gridColumn: professionalIndex + 2, gridRow: `${row} / span ${span}`, '--minute-offset': minute === 30 ? '21px' : '0px' }} onClick={() => onOpenAppointment(appointment)}><strong>{service?.name ?? appointment.serviceName ?? 'Servicio'}</strong><span>{appointment.time}</span><small>{customer?.name ?? appointment.customerName ?? 'Cliente'}</small></button>
            })}
          </div>
        </div>
      )}
      <footer className="calendar-legend"><span><i className="confirmed" />Confirmada</span><span><i className="pending" />Pendiente</span><small>Selecciona un espacio libre para crear una cita</small></footer>
    </article>
  )
}

function AppointmentList({ appointments, customers, professionals, services, onEdit, onNew }) {
  const [status, setStatus] = useState('all')
  const [query, setQuery] = useState('')
  const filtered = appointments
    .filter((item) => status === 'all' || item.status === status)
    .filter((item) => (customers.find((customer) => customer.id === item.customerId)?.name ?? item.customerName ?? '').toLowerCase().includes(query.toLowerCase()))
    .sort((a, b) => `${b.date}${b.time}`.localeCompare(`${a.date}${a.time}`))

  return <section className="data-card"><div className="data-toolbar"><div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar cliente..." /></div><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="all">Todos los estados</option><option value="confirmed">Confirmadas</option><option value="pending">Pendientes</option><option value="completed">Completadas</option><option value="cancelled">Canceladas</option><option value="no_show">No asistió</option></select><button className="new-appointment" onClick={onNew}><Icon name="plus" />Nueva cita</button></div><div className="responsive-table"><table><thead><tr><th>Fecha y hora</th><th>Cliente</th><th>Servicio</th><th>Profesional</th><th>Estado</th><th /></tr></thead><tbody>{filtered.map((appointment) => <tr key={appointment.id}><td><strong>{appointment.date}</strong><small>{appointment.time}</small></td><td>{customers.find((item) => item.id === appointment.customerId)?.name ?? appointment.customerName}</td><td>{services.find((item) => item.id === appointment.serviceId)?.name ?? appointment.serviceName}</td><td>{professionals.find((item) => item.id === appointment.professionalId)?.name ?? appointment.professionalName}</td><td><span className={`table-status ${appointment.status}`}>{statusLabel(appointment.status)}</span></td><td><button className="row-action" onClick={() => onEdit(appointment)}>Editar</button></td></tr>)}</tbody></table>{filtered.length === 0 && <div className="empty-state">No hay citas que coincidan con los filtros.</div>}</div></section>
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

function ReportsView({ negocioId }) {
  const [reporte, setReporte] = useState(null)
  const [loadingReporte, setLoadingReporte] = useState(true)
  const [reporteError, setReporteError] = useState(null)

  useEffect(() => {
    if (!negocioId) return
    let active = true
    setLoadingReporte(true)
    setReporteError(null)
    const hasta = new Date()
    const desde = addDays(hasta, -6)
    obtenerReporte({ negocioId, desde: toDateKey(desde), hasta: toDateKey(hasta) })
      .then((data) => { if (active) setReporte(data) })
      .catch((err) => active && setReporteError(err.message))
      .finally(() => active && setLoadingReporte(false))
    return () => { active = false }
  }, [negocioId])

  if (loadingReporte) return <section className="empty-panel">Cargando reportes...</section>
  if (reporteError) return <section className="empty-panel error-panel"><h2>No pudimos cargar los reportes.</h2><p>{reporteError}</p></section>
  if (!reporte) return null

  const ocupacionPorDia = Object.fromEntries(reporte.ocupacionSemanal.map((item) => [item.diaSemana, item.porcentaje]))

  return <section className="reports-view"><div className="report-summary"><article><small>Ingresos estimados</small><strong>RD$ {reporte.ingresosTotales.toLocaleString()}</strong><span>Últimos 7 días</span></article><article><small>Tasa de confirmación</small><strong>{reporte.tasaConfirmacion}%</strong><span>Citas confirmadas o completadas</span></article><article><small>Servicios agendados</small><strong>{reporte.serviciosAgendados}</strong><span>En el período visible</span></article></div><article className="chart-card"><div><span className="section-kicker">RENDIMIENTO</span><h2>Ocupación semanal</h2></div><div className="bar-chart">{diaOrden.map((dia) => <div key={dia}><span style={{ height: `${ocupacionPorDia[dia] ?? 0}%` }} /><small>{diaLabels[dia]}</small></div>)}</div></article></section>
}

function SettingsView({ settings, setSettings }) {
  function toggle(key) { setSettings((current) => ({ ...current, [key]: !current[key] })) }
  const options = [['reminders', 'Recordatorios automáticos', 'Enviar recordatorios antes de cada cita.'], ['confirmations', 'Confirmación de citas', 'Solicitar confirmación al cliente.'], ['compactCalendar', 'Calendario compacto', 'Reducir la altura de los bloques del calendario.']]
  return <section className="settings-card"><div className="settings-heading"><h2>Preferencias del sistema</h2><p>Estos ajustes se guardan localmente en este navegador.</p></div>{options.map(([key, title, description]) => <div className="setting-row" key={key}><div><strong>{title}</strong><p>{description}</p></div><button className={`toggle-switch ${settings[key] ? 'on' : ''}`} onClick={() => toggle(key)} aria-pressed={settings[key]}><span /></button></div>)}</section>
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
  const [businesses, setBusinesses] = useState([])
  const [selectedBusinessId, setSelectedBusinessId] = useState('')
  const [branches, setBranches] = useState([])
  const [customers, setCustomers] = useState([])
  const [professionals, setProfessionals] = useState([])
  const [services, setServices] = useState([])
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
  const revenue = dayAppointments.reduce((sum, item) => sum + (item.total ?? services.find((service) => service.id === item.serviceId)?.price ?? 0), 0)

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
    ])
      .then(([catalog, citas, empleados, clientes, servicios]) => {
        if (!active) return
        const mappedCatalog = mapCatalog(catalog)
        setBranches(mappedCatalog.branches)
        setCustomers(mappedCatalog.customers)
        setProfessionals(mappedCatalog.professionals)
        setServices(mappedCatalog.services)
        setAppointments((citas ?? []).map(mapAppointment))
        setTeamMembers((empleados ?? []).map(mapEmployee))
        setClientsList((clientes ?? []).map(mapClient))
        setServiceList((servicios ?? []).map(mapServiceItem))
      })
      .catch((err) => active && setError(err.message))
      .finally(() => active && setAppointmentsLoading(false))

    return () => { active = false }
  }, [selectedBusinessId, dateKey])

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
      const catalog = await obtenerCatalogos(selectedBusinessId)
      setServices(mapCatalog(catalog).services)
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
    } catch (err) {
      setEmployeeModalError(err.message)
    } finally {
      setSavingEmployee(false)
    }
  }

  async function refreshAppointments() {
    if (!selectedBusinessId) return
    const citas = await buscarCitas({ negocioId: selectedBusinessId, desde: dateKey, hasta: dateKey })
    setAppointments((citas ?? []).map(mapAppointment))
  }

  function selectSection(section) { setActiveSection(section); setMenuOpen(false) }

  function openNewAppointment(preset = {}) {
    setModalError(null)
    setModalAppointment({
      branchId: branches[0]?.id ?? '',
      date: dateKey,
      time: '09:00',
      duration: services[0]?.duration ?? 60,
      status: 'confirmed',
      notes: '',
      customerId: customers[0]?.id ?? '',
      serviceId: services[0]?.id ?? '',
      professionalId: professionals[0]?.id ?? '',
      ...preset,
    })
  }

  async function saveAppointment(form) {
    setSavingAppointment(true)
    setModalError(null)
    try {
      const payload = buildAppointmentPayload(form, selectedBusinessId)
      if (form.id) {
        await actualizarCita(form.id, payload)
      } else {
        await crearCita(payload)
      }
      setSelectedDate(new Date(`${form.date}T12:00:00`))
      setModalAppointment(null)
      await refreshAppointments()
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
      await eliminarCita(id)
      setModalAppointment(null)
      await refreshAppointments()
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
    if (activeSection === 'calendar') return <Schedule expanded selectedDate={selectedDate} appointments={appointments} professionals={professionals} customers={customers} services={services} onChangeDate={setSelectedDate} onOpenAppointment={openNewAppointment} />
    if (activeSection === 'appointments') return <AppointmentList appointments={appointments} customers={customers} professionals={professionals} services={services} onEdit={setModalAppointment} onNew={() => openNewAppointment()} />
    if (activeSection === 'team') return <TeamView employees={teamMembers} onEdit={setModalEmployee} onNew={openNewEmployee} />
    if (activeSection === 'customers') return <ClientsView clients={clientsList} onEdit={setModalClient} onNew={openNewClient} />
    if (activeSection === 'services') return <ServicesView services={serviceList} onEdit={setModalService} onNew={openNewService} />
    if (activeSection === 'reports') return <ReportsView negocioId={selectedBusinessId} />
    if (activeSection === 'settings') return <SettingsView settings={settings} setSettings={setSettings} />
    return <><section className="stats-grid"><article className="stat-card"><span className="stat-icon green"><Icon name="calendar" /></span><div><small>Citas del día</small><strong>{dayAppointments.length}</strong><p><b>{dayAppointments.filter((item) => item.status === 'confirmed').length}</b> confirmadas</p></div></article><article className="stat-card"><span className="stat-icon dark"><Icon name="customers" /></span><div><small>Clientes agendados</small><strong>{new Set(dayAppointments.map((item) => item.customerId)).size}</strong><p>Agenda seleccionada</p></div></article><article className="stat-card"><span className="stat-icon soft"><Icon name="reports" /></span><div><small>Ingresos estimados</small><strong>RD$ {revenue.toLocaleString()}</strong><p>Servicios no cancelados</p></div></article><article className="stat-card"><span className="stat-icon amber"><Icon name="team" /></span><div><small>Personal activo</small><strong>{professionals.length}</strong><p>Profesionales del negocio</p></div></article></section><section className="workspace-grid"><Schedule selectedDate={selectedDate} appointments={appointments} professionals={professionals} customers={customers} services={services} onChangeDate={setSelectedDate} onOpenAppointment={openNewAppointment} /><aside className="today-card"><div className="card-heading"><div><span className="section-kicker">PRÓXIMAS</span><h2>Citas por atender</h2></div></div>{appointmentsLoading && <div className="empty-state compact">Actualizando agenda...</div>}{!appointmentsLoading && dayAppointments.sort((a, b) => a.time.localeCompare(b.time)).slice(0, 5).map((appointment) => <button className="next-appointment" key={appointment.id} onClick={() => setModalAppointment(appointment)}><span className="appointment-time">{appointment.time}</span><div><strong>{customers.find((item) => item.id === appointment.customerId)?.name ?? appointment.customerName}</strong><p>{services.find((item) => item.id === appointment.serviceId)?.name ?? appointment.serviceName} · {professionals.find((item) => item.id === appointment.professionalId)?.shortName ?? appointment.professionalName}</p></div><Icon name="chevron" /></button>)}{!appointmentsLoading && dayAppointments.length === 0 && <div className="empty-state compact">No hay citas para este día.</div>}<button className="view-all-button" onClick={() => selectSection('appointments')}>Ver todas las citas <Icon name="chevron" /></button><div className="occupancy-card"><div><span>Ocupación del día</span><strong>{Math.min(100, Math.round(dayAppointments.length / 16 * 100))}%</strong></div><div className="progress-track"><span style={{ width: `${Math.min(100, dayAppointments.length / 16 * 100)}%` }} /></div><p>{dayAppointments.length} citas registradas</p></div></aside></section></>
  }

  return <main className="dashboard-page">{menuOpen && <button className="sidebar-backdrop" aria-label="Cerrar menú" onClick={() => setMenuOpen(false)} />}<aside className={`dashboard-sidebar ${menuOpen ? 'open' : ''}`}><div className="sidebar-logo-wrap"><img src={glowUpLogo} alt="GlowUp RD" className="sidebar-logo" /></div><nav className="sidebar-nav" aria-label="Navegación principal"><span className="nav-label">GESTIÓN</span>{navigation.slice(0, 6).map(([key, label]) => <button key={key} className={activeSection === key ? 'active' : ''} onClick={() => selectSection(key)}><Icon name={key} /><span>{label}</span></button>)}<span className="nav-label nav-label-secondary">ANÁLISIS</span>{navigation.slice(6).map(([key, label]) => <button key={key} className={activeSection === key ? 'active' : ''} onClick={() => selectSection(key)}><Icon name={key} /><span>{label}</span></button>)}</nav><div className="sidebar-user"><span className="user-avatar">{initials || 'GU'}</span><span className="user-info"><strong>{user.nombre} {user.apellido}</strong><small>{user.correo}</small></span><button onClick={() => setLogoutConfirmationOpen(true)} title="Cerrar sesión" aria-label="Cerrar sesión"><Icon name="logout" /></button></div></aside><section className="dashboard-content"><header className="dashboard-header"><div className="header-title-wrap"><button className="mobile-menu-button" onClick={() => setMenuOpen(true)} aria-label="Abrir menú"><Icon name="menu" /></button><div><span className="page-kicker">{activeSection === 'dashboard' ? 'PANEL PRINCIPAL' : 'GESTIÓN'}</span><h1>{activeSection === 'dashboard' ? `Buenos días, ${user.nombre}` : sectionTitles[activeSection]}</h1><p className="current-date">{formatLongDate(selectedDate)}</p></div></div><div className="header-actions">{businesses.length > 1 && <select className="business-selector" value={selectedBusinessId} onChange={(event) => setSelectedBusinessId(event.target.value)}>{businesses.map((business) => <option key={business.id} value={business.id}>{business.nombre}</option>)}</select>}<div className="notification-wrap"><button className="notification-button" aria-label="Notificaciones" onClick={() => setNotificationsOpen((open) => !open)}><Icon name="bell" /><span /></button>{notificationsOpen && <div className="notification-popover"><strong>Notificaciones</strong><p>Tienes {dayAppointments.filter((item) => item.status === 'pending').length} citas pendientes de confirmar.</p><button onClick={() => { selectSection('appointments'); setNotificationsOpen(false) }}>Revisar citas</button></div>}</div><button className="new-appointment" onClick={() => openNewAppointment()} disabled={!selectedBusinessId || customers.length === 0 || professionals.length === 0 || services.length === 0}><Icon name="plus" />Nueva cita</button></div></header>{renderMainContent()}</section>{modalAppointment && <AppointmentModal appointment={modalAppointment} branches={branches} customers={customers} professionals={professionals} services={services} saving={savingAppointment} error={modalError} onClose={() => setModalAppointment(null)} onSave={saveAppointment} onDelete={deleteAppointment} />}{modalEmployee && <EmployeeModal employee={modalEmployee} branches={branches} saving={savingEmployee} error={employeeModalError} onClose={() => setModalEmployee(null)} onSave={saveEmployee} />}{modalClient && <ClientModal client={modalClient} saving={savingClient} error={clientModalError} onClose={() => setModalClient(null)} onSave={saveClient} />}{modalService && <ServiceModal service={modalService} saving={savingService} error={serviceModalError} onClose={() => setModalService(null)} onSave={saveService} />}{logoutConfirmationOpen && <LogoutConfirmation onCancel={() => setLogoutConfirmationOpen(false)} onConfirm={onLogout} />}</main>
}

export default Dashboard
