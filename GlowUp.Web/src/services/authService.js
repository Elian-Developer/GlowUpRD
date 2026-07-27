import { apiRequest, clearStoredSession, readStoredSession, SESSION_KEY } from './apiClient'

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

async function iniciarSesionLocal(credentials) {
  const correo = credentials.correo.trim().toLowerCase()
  const user = readDemoUsers().find((item) => item.correo === correo)

  if (!user || user.passwordHash !== await hashPassword(credentials.password, user.salt)) {
    throw new Error('El correo o la contraseña no son correctos.')
  }

  return {
    token: `demo-${crypto.randomUUID()}`,
    expiraEnUtc: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
    usuario: toPublicUser(user),
  }
}

async function registrarUsuarioLocal(user) {
  const users = readDemoUsers()
  const correo = user.correo.trim().toLowerCase()

  if (users.some((item) => item.correo === correo)) {
    throw new Error('El correo ya se encuentra registrado en este navegador.')
  }

  const saltBytes = crypto.getRandomValues(new Uint8Array(16))
  const salt = Array.from(saltBytes, (byte) => byte.toString(16).padStart(2, '0')).join('')
  const newUser = {
    id: crypto.randomUUID(),
    nombre: user.nombre.trim(),
    apellido: user.apellido.trim(),
    correo,
    activo: true,
    fechaCreacion: new Date().toISOString(),
    salt,
    passwordHash: await hashPassword(user.password, salt),
  }

  localStorage.setItem(DEMO_USERS_KEY, JSON.stringify([...users, newUser]))
  return toPublicUser(newUser)
}

export function iniciarSesion(credentials) {
  if (AUTH_MODE === 'local') return iniciarSesionLocal(credentials)

  return apiRequest('/api/autenticacion/iniciar-sesion', {
    method: 'POST',
    body: JSON.stringify(credentials),
  })
}

export function registrarUsuario(user) {
  if (AUTH_MODE === 'local') return registrarUsuarioLocal(user)

  return apiRequest('/api/autenticacion/registrar', {
    method: 'POST',
    body: JSON.stringify(user),
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
