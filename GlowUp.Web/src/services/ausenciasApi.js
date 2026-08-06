import { apiRequest } from './apiClient'

export function buscarAusencias({ negocioId, desde, hasta, empleadoId, incluirCanceladas = false }) {
  const params = new URLSearchParams({ negocioId: String(negocioId), desde, hasta })
  if (empleadoId) params.set('empleadoId', String(empleadoId))
  if (incluirCanceladas) params.set('incluirCanceladas', 'true')
  return apiRequest(`/api/ausencias?${params.toString()}`)
}

export function crearAusencia(payload) {
  return apiRequest('/api/ausencias', { method: 'POST', body: JSON.stringify(payload) })
}

export function actualizarAusencia(id, payload) {
  return apiRequest(`/api/ausencias/${id}`, { method: 'PUT', body: JSON.stringify(payload) })
}

export function cancelarAusencia(id) {
  return apiRequest(`/api/ausencias/${id}`, { method: 'DELETE' })
}
