const API_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')
const SESSION_KEY = 'glowup_session'

function clearStoredSession() {
  localStorage.removeItem(SESSION_KEY)
  sessionStorage.removeItem(SESSION_KEY)
  localStorage.removeItem('glowup_token')
  localStorage.removeItem('glowup_user')
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
  if (problem?.errors) {
    const firstValidationError = Object.values(problem.errors).flat()[0]
    if (firstValidationError) return firstValidationError
  }

  if (problem?.title) return problem.title
  if (problem?.detail) return problem.detail
  if (status === 401) return 'El correo o la contraseña no son correctos.'
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
  }
}

export async function apiRequest(path, options = {}) {
  const session = readStoredSession()
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  }

  if (session?.token && !headers.Authorization) {
    headers.Authorization = `Bearer ${session.token}`
  }

  let response

  try {
    response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers,
    })
  } catch {
    throw new ApiError('No pudimos conectar con el servidor. Verifica que la API esté encendida.', 0, null)
  }

  if (response.status === 401) {
    clearStoredSession()
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(getErrorMessage(problem, response.status), response.status, problem)
  }

  if (response.status === 204) return null
  return response.json()
}

export { SESSION_KEY, clearStoredSession, readStoredSession }
