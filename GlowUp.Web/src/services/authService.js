import { clearStoredSession, readStoredSession, SESSION_KEY } from './apiClient'
import { registrarNegocio } from './negociosApi'
import { apiRequest } from './apiClient'

const AUTH_MODE = import.meta.env.VITE_AUTH_MODE ?? 'local'
const DEMO_USERS_KEY = 'glowup_demo_users'

function readDemoUsers() {
  try {
    return JSON.parse(localStorage.getItem(DEMO_USERS_KEY) ?? '[]')
  } catch {
    return []
  }
}

async function hashPassword(password, salt) {
  const data = new TextEncoder().encode(`${salt}:${password}`)
  const hash = await crypto.subtle.digest('SHA-256', data)
  return Array.from(new Uint8Array(hash), (byte) => byte.toString(16).padStart(2, '0')).join('')
}

function toPublicUser(user) {
  return {
    id: user.id,
    nombre: user.nombre,
    apellido: user.apellido,
    correo: user.correo,
    activo: user.activo,
    fechaCreacion: user.fechaCreacion,
  }
}

function crearLoginDemo(user) {
  return {
    token: `demo-${crypto.randomUUID()}`,
    expiraEnUtc: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
    usuario: toPublicUser(user),
  }
}

async function iniciarSesionLocal(credentials) {
  const correo = credentials.correo.trim().toLowerCase()
  const user = readDemoUsers().find((item) => item.correo === correo)

  if (!user || user.passwordHash !== await hashPassword(credentials.password, user.salt)) {
    throw new Error('El correo o la contraseña no son correctos.')
  }

  return crearLoginDemo(user)
}

async function registrarNegocioLocal(payload) {
  const users = readDemoUsers()
  const correo = payload.correoPropietario.trim().toLowerCase()

  if (users.some((item) => item.correo === correo)) {
    throw new Error('El correo ya se encuentra registrado en este navegador.')
  }

  const saltBytes = crypto.getRandomValues(new Uint8Array(16))
  const salt = Array.from(saltBytes, (byte) => byte.toString(16).padStart(2, '0')).join('')
  const newUser = {
    id: crypto.randomUUID(),
    nombre: payload.nombrePropietario.trim(),
    apellido: payload.apellidoPropietario.trim(),
    correo,
    activo: true,
    fechaCreacion: new Date().toISOString(),
    salt,
    passwordHash: await hashPassword(payload.password, salt),
  }

  localStorage.setItem(DEMO_USERS_KEY, JSON.stringify([...users, newUser]))
  return crearLoginDemo(newUser)
}

export function iniciarSesion(credentials) {
  if (AUTH_MODE === 'local') return iniciarSesionLocal(credentials)

  return apiRequest('/api/autenticacion/iniciar-sesion', {
    method: 'POST',
    body: JSON.stringify(credentials),
  })
}

export function registrarNegocioYPropietario(payload) {
  if (AUTH_MODE === 'local') return registrarNegocioLocal(payload)

  return registrarNegocio(payload)
}

export function solicitarRestablecimiento(correo) {
  if (AUTH_MODE === 'local') return Promise.resolve()

  return apiRequest('/api/autenticacion/olvide-password', {
    method: 'POST',
    body: JSON.stringify({ correo }),
  })
}

export function restablecerPassword(token, nuevaPassword) {
  return apiRequest('/api/autenticacion/restablecer-password', {
    method: 'POST',
    body: JSON.stringify({ token, nuevaPassword }),
  })
}

export function iniciarSesionConGoogle(credentialToken, recordarSesion = false) {
  if (AUTH_MODE === 'local') {
    return Promise.reject(new Error('El inicio de sesión con Google no está disponible en el modo de demostración.'))
  }

  return apiRequest('/api/autenticacion/google', {
    method: 'POST',
    body: JSON.stringify({ credentialToken, recordarSesion }),
  })
}

export function guardarSesion(loginResponse, recordar) {
  const storage = recordar ? localStorage : sessionStorage
  const otherStorage = recordar ? sessionStorage : localStorage

  otherStorage.removeItem(SESSION_KEY)
  storage.setItem(SESSION_KEY, JSON.stringify(loginResponse))
}

export function obtenerSesion() {
  const session = readStoredSession()
  if (!session) return null

  if (!session.token || !session.usuario || new Date(session.expiraEnUtc) <= new Date()) {
    cerrarSesion()
    return null
  }

  return session
}

export function cerrarSesion() {
  clearStoredSession()
}

export async function cerrarSesionEnServidor() {
  if (AUTH_MODE !== 'local') {
    try {
      await apiRequest('/api/autenticacion/cerrar-sesion', { method: 'POST', skipSessionRefresh: true })
    } catch {
      // El cierre local continúa aunque la API no esté disponible.
    }
  }
  clearStoredSession()
}

export async function eliminarCuenta(id) {
  if (AUTH_MODE === 'local') {
    const users = readDemoUsers().filter((user) => String(user.id) !== String(id))
    localStorage.setItem(DEMO_USERS_KEY, JSON.stringify(users))
    clearStoredSession()
    return
  }

  await apiRequest(`/api/autenticacion/usuarios/${id}/cuenta`, {
    method: 'DELETE',
    skipSessionRefresh: true,
  })
  clearStoredSession()
}
