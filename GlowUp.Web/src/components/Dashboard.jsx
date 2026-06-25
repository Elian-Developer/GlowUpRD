import { useMemo, useState } from 'react'
import glowUpLogo from '../assets/glowup-rd-logo.png'
import useLocalStorage from '../hooks/useLocalStorage'
import AppointmentModal from './AppointmentModal'
import {
  createInitialAppointments,
  initialCustomers,
  professionals,
  services,
  toDateKey,
} from '../data/dashboardData'
import './Dashboard.css'

const navigation = [
  ['dashboard', 'Resumen'], ['calendar', 'Calendario'], ['appointments', 'Citas'],
  ['customers', 'Clientes'], ['services', 'Servicios'], ['team', 'Personal'],
  ['reports', 'Reportes'], ['settings', 'Configuración'],
]

const hours = ['08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00']
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

function formatLongDate(date) {
  return new Intl.DateTimeFormat('es-DO', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(date)
}

function addDays(date, amount) {
  const next = new Date(date)
  next.setDate(next.getDate() + amount)
  return next
}

function statusLabel(status) {
  return { pending: 'Pendiente', confirmed: 'Confirmada', completed: 'Completada', cancelled: 'Cancelada' }[status]
}

function Schedule({ selectedDate, appointments, onChangeDate, onOpenAppointment, expanded = false }) {
  const [uiSettings] = useLocalStorage('glowup_ui_settings', { reminders: true, confirmations: true, compactCalendar: false })
  const dayAppointments = appointments.filter((item) => item.date === toDateKey(selectedDate) && item.status !== 'cancelled')

  function openSlot(professionalId, hour) {
    onOpenAppointment({
      date: toDateKey(selectedDate), time: hour, professionalId,
      customerId: '', serviceId: '', duration: 60, status: 'confirmed', notes: '',
    })
  }

  return (
    <article className={`schedule-card ${expanded ? 'schedule-expanded' : ''} ${uiSettings.compactCalendar ? 'compact-calendar' : ''}`}>
      <div className="card-heading schedule-heading">
        <div><span className="section-kicker">AGENDA</span><h2>{expanded ? 'Calendario de citas' : 'Calendario de hoy'}</h2><p>{formatLongDate(selectedDate)}</p></div>
        <div className="calendar-controls"><button onClick={() => onChangeDate(addDays(selectedDate, -1))} aria-label="Día anterior">‹</button><button className="today-button" onClick={() => onChangeDate(new Date())}>Hoy</button><button onClick={() => onChangeDate(addDays(selectedDate, 1))} aria-label="Día siguiente">›</button></div>
      </div>
      <div className="calendar-scroll">
        <div className="calendar-board">
          <div className="calendar-corner">Hora</div>
          {professionals.map((professional) => <div className="professional-heading" key={professional.id}><span className={`professional-avatar ${professional.tone}`}>{professional.initials}</span>{professional.shortName}</div>)}
          {hours.map((hour, index) => <div className="time-label" key={hour} style={{ gridRow: index + 2 }}>{hour}</div>)}
          {hours.flatMap((hour, row) => professionals.map((professional, column) => <button aria-label={`Crear cita con ${professional.name} a las ${hour}`} className="calendar-cell" key={`${professional.id}-${hour}`} style={{ gridColumn: column + 2, gridRow: row + 2 }} onClick={() => openSlot(professional.id, hour)} />))}
          {dayAppointments.map((appointment) => {
            const professionalIndex = professionals.findIndex((item) => item.id === appointment.professionalId)
            const [hour, minute] = appointment.time.split(':').map(Number)
            const row = Math.max(2, hour - 8 + 2)
            const span = Math.max(1, Math.ceil(appointment.duration / 60))
            const customer = initialCustomers.find((item) => item.id === appointment.customerId)
            const service = services.find((item) => item.id === appointment.serviceId)
            if (professionalIndex < 0 || row > 11) return null
            return <button className={`appointment-event ${appointment.status}`} key={appointment.id} style={{ gridColumn: professionalIndex + 2, gridRow: `${row} / span ${span}`, '--minute-offset': minute === 30 ? '21px' : '0px' }} onClick={() => onOpenAppointment(appointment)}><strong>{service?.name ?? 'Servicio'}</strong><span>{appointment.time}</span><small>{customer?.name ?? 'Cliente'}</small></button>
          })}
        </div>
      </div>
      <footer className="calendar-legend"><span><i className="confirmed" />Confirmada</span><span><i className="pending" />Pendiente</span><small>Selecciona un espacio libre para crear una cita</small></footer>
    </article>
  )
}

function AppointmentList({ appointments, customers, onEdit, onNew }) {
  const [status, setStatus] = useState('all')
  const [query, setQuery] = useState('')
  const filtered = appointments
    .filter((item) => status === 'all' || item.status === status)
    .filter((item) => (customers.find((customer) => customer.id === item.customerId)?.name ?? '').toLowerCase().includes(query.toLowerCase()))
    .sort((a, b) => `${b.date}${b.time}`.localeCompare(`${a.date}${a.time}`))

  return <section className="data-card"><div className="data-toolbar"><div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar cliente..." /></div><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="all">Todos los estados</option><option value="confirmed">Confirmadas</option><option value="pending">Pendientes</option><option value="completed">Completadas</option><option value="cancelled">Canceladas</option></select><button className="new-appointment" onClick={onNew}><Icon name="plus" />Nueva cita</button></div><div className="responsive-table"><table><thead><tr><th>Fecha y hora</th><th>Cliente</th><th>Servicio</th><th>Profesional</th><th>Estado</th><th /></tr></thead><tbody>{filtered.map((appointment) => <tr key={appointment.id}><td><strong>{appointment.date}</strong><small>{appointment.time}</small></td><td>{customers.find((item) => item.id === appointment.customerId)?.name}</td><td>{services.find((item) => item.id === appointment.serviceId)?.name}</td><td>{professionals.find((item) => item.id === appointment.professionalId)?.name}</td><td><span className={`table-status ${appointment.status}`}>{statusLabel(appointment.status)}</span></td><td><button className="row-action" onClick={() => onEdit(appointment)}>Editar</button></td></tr>)}</tbody></table>{filtered.length === 0 && <div className="empty-state">No hay citas que coincidan con los filtros.</div>}</div></section>
}

function DirectoryView({ type, customers }) {
  const [query, setQuery] = useState('')
  const content = type === 'customers' ? customers.map((item) => ({ ...item, subtitle: item.email, detail: `${item.visits} visitas` })) : type === 'services' ? services.map((item) => ({ ...item, subtitle: item.category, detail: `RD$ ${item.price.toLocaleString()} · ${item.duration} min` })) : professionals.map((item) => ({ ...item, subtitle: item.specialty, detail: 'Disponible hoy' }))
  const filtered = content.filter((item) => (item.name ?? '').toLowerCase().includes(query.toLowerCase()))
  return <section className="directory-view"><div className="data-toolbar"><div className="search-box"><Icon name="search" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Buscar..." /></div><span className="result-count">{filtered.length} resultados</span></div><div className="directory-grid">{filtered.map((item) => <article key={item.id} className="directory-card"><span className="directory-avatar">{item.initials ?? item.name.split(' ').map((part) => part[0]).slice(0, 2).join('')}</span><div><strong>{item.name}</strong><p>{item.subtitle}</p><small>{item.detail}</small></div></article>)}</div></section>
}

function ReportsView({ appointments }) {
  const active = appointments.filter((item) => item.status !== 'cancelled')
  const values = [58, 72, 64, 88, 79, 93, 76]
  return <section className="reports-view"><div className="report-summary"><article><small>Ingresos estimados</small><strong>RD$ {active.reduce((sum, item) => sum + (services.find((service) => service.id === item.serviceId)?.price ?? 0), 0).toLocaleString()}</strong><span>Según las citas registradas</span></article><article><small>Tasa de confirmación</small><strong>{active.length ? Math.round(active.filter((item) => item.status === 'confirmed').length / active.length * 100) : 0}%</strong><span>Citas actualmente confirmadas</span></article><article><small>Servicios agendados</small><strong>{active.length}</strong><span>En el período visible</span></article></div><article className="chart-card"><div><span className="section-kicker">RENDIMIENTO</span><h2>Ocupación semanal</h2></div><div className="bar-chart">{values.map((value, index) => <div key={index}><span style={{ height: `${value}%` }} /><small>{['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'][index]}</small></div>)}</div></article></section>
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
  const [appointments, setAppointments] = useLocalStorage('glowup_appointments', createInitialAppointments())
  const [customers] = useLocalStorage('glowup_customers', initialCustomers)
  const [settings, setSettings] = useLocalStorage('glowup_ui_settings', { reminders: true, confirmations: true, compactCalendar: false })
  const [modalAppointment, setModalAppointment] = useState(null)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [logoutConfirmationOpen, setLogoutConfirmationOpen] = useState(false)
  const user = session.usuario
  const initials = `${user.nombre?.[0] ?? ''}${user.apellido?.[0] ?? ''}`.toUpperCase()
  const dayAppointments = useMemo(() => appointments.filter((item) => item.date === toDateKey(selectedDate) && item.status !== 'cancelled'), [appointments, selectedDate])
  const revenue = dayAppointments.reduce((sum, item) => sum + (services.find((service) => service.id === item.serviceId)?.price ?? 0), 0)

  function selectSection(section) { setActiveSection(section); setMenuOpen(false) }
  function openNewAppointment(preset = {}) { setModalAppointment({ date: toDateKey(selectedDate), time: '09:00', duration: 60, status: 'confirmed', notes: '', customerId: '', serviceId: '', professionalId: professionals[0].id, ...preset }) }
  function saveAppointment(form) { setAppointments((current) => form.id ? current.map((item) => item.id === form.id ? form : item) : [...current, { ...form, id: crypto.randomUUID() }]); setSelectedDate(new Date(`${form.date}T12:00:00`)); setModalAppointment(null) }
  function deleteAppointment(id) { setAppointments((current) => current.filter((item) => item.id !== id)); setModalAppointment(null) }

  function renderMainContent() {
    if (activeSection === 'calendar') return <Schedule expanded selectedDate={selectedDate} appointments={appointments} onChangeDate={setSelectedDate} onOpenAppointment={setModalAppointment} />
    if (activeSection === 'appointments') return <AppointmentList appointments={appointments} customers={customers} onEdit={setModalAppointment} onNew={() => openNewAppointment()} />
    if (['customers', 'services', 'team'].includes(activeSection)) return <DirectoryView type={activeSection} customers={customers} />
    if (activeSection === 'reports') return <ReportsView appointments={appointments} />
    if (activeSection === 'settings') return <SettingsView settings={settings} setSettings={setSettings} />
    return <><section className="stats-grid"><article className="stat-card"><span className="stat-icon green"><Icon name="calendar" /></span><div><small>Citas del día</small><strong>{dayAppointments.length}</strong><p><b>{dayAppointments.filter((item) => item.status === 'confirmed').length}</b> confirmadas</p></div></article><article className="stat-card"><span className="stat-icon dark"><Icon name="customers" /></span><div><small>Clientes agendados</small><strong>{new Set(dayAppointments.map((item) => item.customerId)).size}</strong><p>Agenda seleccionada</p></div></article><article className="stat-card"><span className="stat-icon soft"><Icon name="reports" /></span><div><small>Ingresos estimados</small><strong>RD$ {revenue.toLocaleString()}</strong><p>Servicios no cancelados</p></div></article><article className="stat-card"><span className="stat-icon amber"><Icon name="team" /></span><div><small>Personal activo</small><strong>{professionals.length}</strong><p>Todos disponibles</p></div></article></section><section className="workspace-grid"><Schedule selectedDate={selectedDate} appointments={appointments} onChangeDate={setSelectedDate} onOpenAppointment={setModalAppointment} /><aside className="today-card"><div className="card-heading"><div><span className="section-kicker">PRÓXIMAS</span><h2>Citas por atender</h2></div></div>{dayAppointments.sort((a, b) => a.time.localeCompare(b.time)).slice(0, 5).map((appointment) => <button className="next-appointment" key={appointment.id} onClick={() => setModalAppointment(appointment)}><span className="appointment-time">{appointment.time}</span><div><strong>{customers.find((item) => item.id === appointment.customerId)?.name}</strong><p>{services.find((item) => item.id === appointment.serviceId)?.name} · {professionals.find((item) => item.id === appointment.professionalId)?.shortName}</p></div><Icon name="chevron" /></button>)}{dayAppointments.length === 0 && <div className="empty-state compact">No hay citas para este día.</div>}<button className="view-all-button" onClick={() => selectSection('appointments')}>Ver todas las citas <Icon name="chevron" /></button><div className="occupancy-card"><div><span>Ocupación del día</span><strong>{Math.min(100, Math.round(dayAppointments.length / 16 * 100))}%</strong></div><div className="progress-track"><span style={{ width: `${Math.min(100, dayAppointments.length / 16 * 100)}%` }} /></div><p>{dayAppointments.length} citas registradas</p></div></aside></section></>
  }

  return <main className="dashboard-page">{menuOpen && <button className="sidebar-backdrop" aria-label="Cerrar menú" onClick={() => setMenuOpen(false)} />}<aside className={`dashboard-sidebar ${menuOpen ? 'open' : ''}`}><div className="sidebar-logo-wrap"><img src={glowUpLogo} alt="GlowUp RD" className="sidebar-logo" /></div><nav className="sidebar-nav" aria-label="Navegación principal"><span className="nav-label">GESTIÓN</span>{navigation.slice(0, 6).map(([key, label]) => <button key={key} className={activeSection === key ? 'active' : ''} onClick={() => selectSection(key)}><Icon name={key} /><span>{label}</span></button>)}<span className="nav-label nav-label-secondary">ANÁLISIS</span>{navigation.slice(6).map(([key, label]) => <button key={key} className={activeSection === key ? 'active' : ''} onClick={() => selectSection(key)}><Icon name={key} /><span>{label}</span></button>)}</nav><div className="sidebar-user"><span className="user-avatar">{initials || 'GU'}</span><span className="user-info"><strong>{user.nombre} {user.apellido}</strong><small>{user.correo}</small></span><button onClick={() => setLogoutConfirmationOpen(true)} title="Cerrar sesión" aria-label="Cerrar sesión"><Icon name="logout" /></button></div></aside><section className="dashboard-content"><header className="dashboard-header"><div className="header-title-wrap"><button className="mobile-menu-button" onClick={() => setMenuOpen(true)} aria-label="Abrir menú"><Icon name="menu" /></button><div><span className="page-kicker">{activeSection === 'dashboard' ? 'PANEL PRINCIPAL' : 'GESTIÓN'}</span><h1>{activeSection === 'dashboard' ? `Buenos días, ${user.nombre}` : sectionTitles[activeSection]}</h1><p className="current-date">{formatLongDate(selectedDate)}</p></div></div><div className="header-actions"><div className="notification-wrap"><button className="notification-button" aria-label="Notificaciones" onClick={() => setNotificationsOpen((open) => !open)}><Icon name="bell" /><span /></button>{notificationsOpen && <div className="notification-popover"><strong>Notificaciones</strong><p>Tienes {dayAppointments.filter((item) => item.status === 'pending').length} citas pendientes de confirmar.</p><button onClick={() => { selectSection('appointments'); setNotificationsOpen(false) }}>Revisar citas</button></div>}</div><button className="new-appointment" onClick={() => openNewAppointment()}><Icon name="plus" />Nueva cita</button></div></header>{renderMainContent()}</section>{modalAppointment && <AppointmentModal appointment={modalAppointment} customers={customers} professionals={professionals} services={services} onClose={() => setModalAppointment(null)} onSave={saveAppointment} onDelete={deleteAppointment} />}{logoutConfirmationOpen && <LogoutConfirmation onCancel={() => setLogoutConfirmationOpen(false)} onConfirm={onLogout} />}</main>
}

export default Dashboard
