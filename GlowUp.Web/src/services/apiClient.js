const API_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')
const SESSION_KEY = 'glowup_session'

function clearStoredSession() {
  localStorage.removeItem(SESSION_KEY)
  sessionStorage.removeItem(SESSION_KEY)
  localStorage.removeItem('glowup_token')
  localStorage.removeItem('glowup_user')
  window.dispatchEvent(new Event('glowup:session-ended'))
}

function readStoredSession() {
  const storedSession =
    localStorage.getItem(SESSION_KEY) ?? sessionStorage.getItem(SESSION_KEY)

  if (!storedSession) return null

  try {
    return JSON.parse(storedSession)
  } catch {
    clearStoredSession()
    return null
  }
}

function getErrorMessage(problem, status) {
  if (Array.isArray(problem?.errors) && problem.errors[0]?.message) {
    return problem.errors[0].message
  }

  if (problem?.errors && !Array.isArray(problem.errors)) {
    const firstValidationError = Object.values(problem.errors).flat()[0]
    if (firstValidationError) return firstValidationError
  }

  if (problem?.title) return problem.title
  if (problem?.detail) return problem.detail
  if (status === 401) return 'Tu sesión expiró. Inicia sesión nuevamente.'
  if (status === 403) return 'No tienes permiso para realizar esta acción.'
  if (status === 404) return 'La ruta solicitada no existe en la API.'
  if (status >= 500) return 'La API encontró un error interno. Revisa la consola del servidor.'
  return 'Ocurrió un error inesperado. Inténtalo nuevamente.'
}

export class ApiError extends Error {
  constructor(message, status, problem) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    this.errors = Array.isArray(problem?.errors) ? problem.errors : []
  }

  fieldError(field) {
    return this.errors.find((error) => error.field === field)?.message ?? null
  }
}

let refreshInFlight = null

function updateStoredSession(loginResponse) {
  const storage = localStorage.getItem(SESSION_KEY) ? localStorage : sessionStorage
  const current = readStoredSession()
  if (!current) return false
  const updated = { ...current, ...loginResponse }
  storage.setItem(SESSION_KEY, JSON.stringify(updated))
  window.dispatchEvent(new CustomEvent('glowup:session-refreshed', { detail: updated }))
  return true
}

async function refreshAccessToken() {
  if (!readStoredSession()) return false
  if (!refreshInFlight) {
    refreshInFlight = fetch(`${API_URL}/api/autenticacion/refrescar`, {
      method: 'POST',
      credentials: 'include',
    })
      .then(async (response) => response.ok && updateStoredSession(await response.json()))
      .catch(() => false)
      .finally(() => { refreshInFlight = null })
  }
  return refreshInFlight
}

function expiresSoon(session) {
  return Boolean(session?.expiraEnUtc) && new Date(session.expiraEnUtc).getTime() <= Date.now() + 30_000
}

async function sendRequest(path, options) {
  const session = readStoredSession()
  const headers = { 'Content-Type': 'application/json', ...options.headers }
  const usesStoredToken = Boolean(session?.token && !headers.Authorization)
  if (usesStoredToken) headers.Authorization = `Bearer ${session.token}`
  const response = await fetch(`${API_URL}${path}`, { ...options, headers, credentials: 'include' })
  return { response, usesStoredToken }
}

export async function apiRequest(path, options = {}) {
  const { skipSessionRefresh = false, ...requestOptions } = options
  const canRefresh = !skipSessionRefresh && path !== '/api/autenticacion/refrescar' && path !== '/api/autenticacion/cerrar-sesion'
  let result

  try {
    if (canRefresh && expiresSoon(readStoredSession())) await refreshAccessToken()
    result = await sendRequest(path, requestOptions)
    if (result.response.status === 401 && result.usesStoredToken && canRefresh && await refreshAccessToken()) {
      result = await sendRequest(path, requestOptions)
    }
  } catch {
    throw new ApiError('No pudimos conectar con el servidor. Verifica que la API esté encendida.', 0, null)
  }

  const { response } = result
  if (response.status === 401) clearStoredSession()

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(getErrorMessage(problem, response.status), response.status, problem)
  }

  if (response.status === 204) return null
  return response.json()
}

export { SESSION_KEY, clearStoredSession, readStoredSession }
